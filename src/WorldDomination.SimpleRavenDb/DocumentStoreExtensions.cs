namespace WorldDomination.SimpleRavenDb
{
    public static class DocumentStoreExtensions
    {
        // NOTE: Ignore DatabaseDoesNotExistException or AuthorizationException exceptions.
        private static AsyncPolicy CheckRavenDbPolicyAsync(ILogger logger, CancellationToken cancellationToken)
        {
            // NOTE: we ignore:
            //         - if the database doesn't exist 
            //         - if we cannot authorize against the db
            //       because we manually handle this outside of the retry. i.e. we don't need to retry, at all.
            return Policy
                .Handle<Exception>(exception => exception is not DatabaseDoesNotExistException &&
                                                exception is not AuthorizationException)
                .WaitAndRetryAsync(15, _ => TimeSpan.FromSeconds(2), (exception, timeSpan, context) =>
                {
                    var message = exception.Message.ToFirstNewLineOrDefault();
                    logger.LogWarning("Failed to connect to RavenDb. It may not be ready. Retrying ... time to wait: {timeSpan}. Exception: {exceptionMessage}",
                        timeSpan,
                        $"{exception.GetType()} {message}");

                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogWarning("  !!! Request for graceful shutdown - so we're going to stop trying ....");
                        throw new OperationCanceledException();
                    }
                });
        }
        private static AsyncPolicy SetupRavenDbPolicyAsync(ILogger logger, CancellationToken cancellationToken)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(15, _ => TimeSpan.FromSeconds(2), (exception, timeSpan, __) =>
                {
                    var message = exception.Message.ToFirstNewLineOrDefault();
                    logger.LogWarning("Failed to setup RavenDb. Retrying ... time to wait: {timeSpan}. Exception: {exceptionMessage}", 
                        timeSpan, 
                        $"{exception.GetType()} {message}");

                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogWarning("  !!! Request for graceful shutdown - so we're going to stop trying ....");
                        throw new OperationCanceledException();
                    }
                });
        }

        /// <summary>
        /// Setup a RavenDb database, ready to be used with some seeded, fake data (if provided) and any indexes (if provided).
        /// </summary>
        /// <param name="documentStore">DocumentStore to check.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Task SetupRavenDbAsync(
            this IDocumentStore documentStore,
            ILogger logger,
            CancellationToken cancellationToken) => documentStore.SetupRavenDbAsync(null, logger, cancellationToken);

        /// <summary>
        /// Setup a RavenDb database, ready to be used with some seeded, fake data (if provided) and any indexes (if provided).
        /// </summary>
        /// <param name="documentStore">DocumentStore to check.</param>
        /// <param name="setupOptions">Optional: any custom setup options, like data to seed.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>Default Polly policy is 15 checks, every 2 seconds (if none was provided).</remarks>
        public static async Task SetupRavenDbAsync(
            this IDocumentStore documentStore,
            RavenDbSetupOptions? setupOptions,
            ILogger logger, 
            CancellationToken cancellationToken)
        {
            logger.LogDebug(" - Checking if the database '{setupDatabaseName}' exists and if any data exists...", documentStore.Database);

            // First, lets grab some common/standard DB statistics. This way, we can determine if there is a DB
            // and if the DB has data, etc.
            // Result:
            //     - No Statistics == no DB and we'll need to create one.
            //     - Statistics == check to see if there is any data.
            //                     If we have some, nothing to do.
            //                     Otherwise, add fake seed data.
            DatabaseStatistics? existingDatabaseStatistics;
            try
            {
                // If a custom policy is provided, use that. Otherwise, we default back to a default-policy.
                var checkRavenDbPolicy = setupOptions?.Policy ?? CheckRavenDbPolicyAsync(logger, cancellationToken);

                existingDatabaseStatistics = await checkRavenDbPolicy
                    .ExecuteAsync(
                        async actionCancellationToken => await documentStore.Maintenance.SendAsync(
                            new GetStatisticsOperation(),
                            actionCancellationToken),
                        cancellationToken);
            }
            catch (DatabaseDoesNotExistException)
            {
                existingDatabaseStatistics = null; // No statistics because there's no Db tenant.
            }
            catch (AuthorizationException)
            {
                // We failed to authenticate against the database. This _usually_ means that we
                // probably didn't have ADMIN rights against a db (so we can't do high level stuff)
                // but we could still do other stuff, like add fake data for seeding the DB.
                // SUMMARY: Db Tenant might exist, so lets assume that it does (safer bet).
                existingDatabaseStatistics = new DatabaseStatistics();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Cancelling setting up RavenDb database ...");
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Now that we've checked the Db, lets create a DB and/or seed data, based on the Statistics values.
            try
            {
                await SetupRavenDbPolicyAsync(logger, cancellationToken)
                    .ExecuteAsync(async actionCancellationToken => await DoSetupRavenDbDelegateAsync(
                        documentStore,
                        setupOptions,
                        existingDatabaseStatistics,
                        logger,
                        actionCancellationToken),
                        cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError("Failed to setup RavenDb and all retries failed. Unable to continue. Application is now terminating. Error: {exception}", exception);
                throw;
            }
        }

        /// <summary>
        /// Seeds some fake data into an existing database.
        /// </summary>
        /// <param name="documentStore">DocumentStore to seed data into.</param>
        /// <param name="documentCollections">Collection of documents.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Task SeedFakeDataAsync(this IDocumentStore documentStore,
            IList documentCollections,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            return SeedCollectionsOfFakeDataAsync(documentStore,
                new List<IList> { documentCollections },
                logger,
                cancellationToken);
        }

        private static async Task DoSetupRavenDbDelegateAsync(
            IDocumentStore documentStore,
            RavenDbSetupOptions? setupOptions,
            DatabaseStatistics? existingDatabaseStatistics,
            ILogger logger,
            CancellationToken actionCancellationToken)
        {
            // Create a new DB (which happens if we have no Statistics).
            if (existingDatabaseStatistics != null)
            {
                logger.LogDebug(" - Database exists: no need to create one.");
            }
            else
            {
                await documentStore.SetupDatabaseTenantAsync(
                    logger,
                    actionCancellationToken);
            }

            if (actionCancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Cancelling setting up RavenDb database ...");
                actionCancellationToken.ThrowIfCancellationRequested();
            }

            // Now add any seed data if we don't have any Documents.
            var documentCount = existingDatabaseStatistics?.CountOfDocuments ?? 0;
            if (documentCount > 0)
            {
                var count = $"{documentCount:N0}";
                logger.LogDebug(" - Skipping seeding fake data because database has {setupDocumentCount} documents already in it.", count);
            }
            else
            {
                var seedDocuments = setupOptions?.DocumentCollections ?? Enumerable.Empty<IList>();

                await documentStore.SeedCollectionsOfFakeDataAsync(
                    seedDocuments,
                    logger,
                    actionCancellationToken);
            }

            if (actionCancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Cancelling setting up RavenDb database ...");
                actionCancellationToken.ThrowIfCancellationRequested();
            }

            // Finally - any indexes we can pre-generate.
            if (setupOptions?.IndexAssembly != null)
            {
                await documentStore.SetupIndexesAsync(
                    setupOptions.IndexAssembly,
                    logger,
                    actionCancellationToken);
            }
        }

        /// <summary>
        /// Seeds some fake data into an existing database.
        /// </summary>
        /// <param name="documentStore">DocumentStore to seed data into.</param>
        /// <param name="documentCollections">Collection of document-collections.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task SeedCollectionsOfFakeDataAsync(
            this IDocumentStore documentStore,
            IEnumerable<IList> documentCollections,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            if (documentCollections is null ||
                !documentCollections.Any())
            {
                logger.LogDebug(" - Skipping seeding fake data because no fake data was provided.");
                return;
            }

            var documentCollectionCount = 0;
            logger.LogDebug(" - Seeding fake data ....");

            foreach (var documentCollection in documentCollections)
            {
                documentCollectionCount++;

                // Determine if we should be bulk inserting. 
                // We bulk insert if the number of documents _per collection_ are 
                // what I personally would consider is "large".
                if (documentCollection.Count > 1000)
                {
                    await documentStore.SeedLargeDataSetAsync(documentCollection);
                }
                else
                {
                    await documentStore.SeedSmallDataSetAsync(documentCollection, cancellationToken);
                }
            }

            logger.LogDebug(" - Finished. Found {documentCollectionCount} document collections and stored {documentsCount} documents.", 
                documentCollectionCount,
                documentCollections.Sum(documents => documents.Count));
        }
        private static async Task SetupDatabaseTenantAsync(
            this IDocumentStore documentStore,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            // Create the db if it doesn't exist.
            // This will mainly occur for in memory localhost development.
            logger.LogDebug(" - ** No database tenant found so creating a new database tenant ....");

            var databaseRecord = new DatabaseRecord(documentStore.Database);
            var createDbOperation = new CreateDatabaseOperation(databaseRecord);

            // Race condition might occur. Meaning, two services might have both seen that the Database-tenant 
            // didn't exist but then they both try and create the Database-tenant, now.
            // Database-tenant can only be created once.
            try
            {
                await documentStore.Maintenance.Server.SendAsync(createDbOperation, cancellationToken);
            }
            catch (ConcurrencyException)
            {
                logger.LogInformation(
                    " - !!! Race-condition captured :: tried to create a database tenant '{setupDatabaseName}' but another service already created it.",
                    documentStore.Database);
            }
        }
        private static async Task SeedSmallDataSetAsync(
            this IDocumentStore documentStore,
            IEnumerable documentCollection,
            CancellationToken cancellationToken)
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                foreach (var document in documentCollection)
                {
                    await session.StoreAsync(document, cancellationToken);
                }

                await session.SaveChangesAsync(cancellationToken);
            }
        }
        private static async Task SeedLargeDataSetAsync(
            this IDocumentStore documentStore,
            IEnumerable documentCollection)
        {
            using (var bulkInsert = documentStore.BulkInsert())
            {
                foreach (var document in documentCollection)
                {
                    await bulkInsert.StoreAsync(document);
                }
            }
        }
        private static async Task SetupIndexesAsync(
            this IDocumentStore documentStore,
            Type indexAssembly,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            logger.LogDebug(" - Creating/Updating indexes : will look for all indexes in the assembly '{indexAssembly}' ...", indexAssembly.FullName);
            await IndexCreation.CreateIndexesAsync(
                indexAssembly.Assembly,
                documentStore,
                token: cancellationToken);
        }
    }
}
