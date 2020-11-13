using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.Exceptions.Security;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace WorldDomination.SimpleRavenDb
{
    public static class DocumentStoreExtensions
    {
        // NOTE: Ignore DatabaseDoesNotExistException or AuthorizationException exceptions.
        private static Policy CheckRavenDbPolicy(ILogger logger)
        {
            return Policy
                .Handle<Exception>(exception => !(exception is DatabaseDoesNotExistException) &&
                                                !(exception is AuthorizationException))
                .WaitAndRetry(15, _ => TimeSpan.FromSeconds(2), (exception, timeSpan, context) =>
                {
                    logger.LogWarning($"Failed to connect to RavenDb. It may not be ready. Retrying ... time to wait: {timeSpan}. Exception: {exception.GetType()} {exception.Message.Substring(0, exception.Message.IndexOf('\n'))}");
                });
        }

        private static AsyncRetryPolicy SetupRavenDbPolicy(CancellationToken cancellationToken, ILogger logger)
        {
            return Policy
                .Handle<Exception>(exception => !(exception is ConcurrencyException)) // Race Condition: DB already exists.
                .WaitAndRetryAsync(15, _ => TimeSpan.FromSeconds(2), (exception, timeSpan, __) =>
                {
                    var message = exception.Message.Contains("\n")

                        ? exception.Message.Substring(0, exception.Message.IndexOf('\n'))
                        : exception.Message;
                    logger.LogWarning($"Failed to setup RavenDb. Retrying ... time to wait: {timeSpan}. Exception: {exception.GetType()} {message}");

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
        public static Task SetupRavenDbAsync(this IDocumentStore documentStore,
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
        public static async Task SetupRavenDbAsync(this IDocumentStore documentStore,
            RavenDbSetupOptions setupOptions,
            ILogger logger, 
            CancellationToken cancellationToken)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.LogDebug($" - Checking if the database '{documentStore.Database}' exists and if any data exists...");

            DatabaseStatistics existingDatabaseStatistics = null;
            try
            {
                var checkRavenDbPolicy = setupOptions?.Policy ?? CheckRavenDbPolicy(logger);
                await checkRavenDbPolicy.Execute(async token =>
                {
                    existingDatabaseStatistics = await documentStore.Maintenance.SendAsync(new GetStatisticsOperation(), cancellationToken);
                }, cancellationToken);
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

            try
            {
                await SetupRavenDbPolicy(cancellationToken, logger).ExecuteAsync(async token =>
                {
                    await SetupDatabaseTenantAsync(documentStore,
                        existingDatabaseStatistics != null,
                        logger,
                        cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogInformation("Cancelling setting up RavenDb database ...");
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    var documentCount = existingDatabaseStatistics?.CountOfDocuments ?? 0;
                    if (documentCount > 0)
                    {
                        logger.LogDebug($" - Skipping seeding fake data because database has {documentCount:N0} documents already in it.");
                    }
                    else
                    {
                        await SeedCollectionsOfFakeDataAsync(documentStore,
                            setupOptions?.DocumentCollections,
                            logger,
                            cancellationToken);
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogInformation("Cancelling setting up RavenDb database ...");
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await SetupIndexesAsync(documentStore,
                        setupOptions?.IndexAssembly,
                        logger,
                        cancellationToken);
                }, cancellationToken);
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

        /// <summary>
        /// Seeds some fake data into an existing database.
        /// </summary>
        /// <param name="documentStore">DocumentStore to seed data into.</param>
        /// <param name="documentCollections">Collection of document-collections.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task SeedCollectionsOfFakeDataAsync(this IDocumentStore documentStore,
            IEnumerable<IList> documentCollections,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            if (documentStore is null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (documentCollections is null ||
                !documentCollections.Any())
            {
                logger.LogDebug(" - Skipping seeding fake data because no fake data was provided.");
                return;
            }

            int documentCollectionCount = 0;
            logger.LogDebug(" - Seeding fake data ....");

            using (var session = documentStore.OpenAsyncSession())
            {
                foreach (var documentCollection in documentCollections)
                {
                    documentCollectionCount++;

                    // Determine if we should be bulk inserting. 
                    // We bulk insert if the number of documents _per collection_ are 
                    // what I personally would consider is "large".
                    if (documentCollection.Count > 1000)
                    {
                        await SeedLargeDataSetAsync(documentCollection, documentStore);
                    }
                    else
                    {
                        await SeedSmallDataSetAsync(documentCollection, session, cancellationToken);
                    }
                }

                // This saves any changes saved for small document collections.
                await session.SaveChangesAsync(cancellationToken);
            }

            logger.LogDebug(" - Finished. Found {documentCollectionCount} document collections and stored {documentsCount} documents.", 
                documentCollectionCount,
                documentCollections.Sum(documents => documents.Count));
        }

        private static async Task SetupDatabaseTenantAsync(IDocumentStore documentStore,
            bool doesDatabaseExist,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (doesDatabaseExist)
            {
                logger.LogDebug(" - Database exists: no need to create one.");
                return;
            }

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
                logger.LogWarning($" - !!! Race-condition captured :: tried to create a database tenant '{documentStore.Database}' but another service already created it.");
            }
        }

        private static async Task SeedSmallDataSetAsync(IEnumerable documentCollection,
            IAsyncDocumentSession session,
            CancellationToken cancellationToken)
        {
            if (documentCollection is null)
            {
                throw new ArgumentNullException(nameof(documentCollection));
            }

            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            foreach (var document in documentCollection)
            {
                await session.StoreAsync(document, cancellationToken);
            }
        }

        private static async Task SeedLargeDataSetAsync(IEnumerable documentCollection,
            IDocumentStore documentStore)
        {
            if (documentCollection is null)
            {
                throw new ArgumentNullException(nameof(documentCollection));
            }

            if (documentStore is null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }

            using (var bulkInsert = documentStore.BulkInsert())
            {
                foreach (var document in documentCollection)
                {
                    await bulkInsert.StoreAsync(document);
                }
            }
        }

        private static async Task SetupIndexesAsync(IDocumentStore documentStore,
            Type indexAssembly,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            if (documentStore is null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (indexAssembly != null)
            {
                logger.LogDebug(" - Creating/Updating indexes : will look for all indexes in the assembly '{indexAssembly}' ...", indexAssembly.FullName);
                await IndexCreation.CreateIndexesAsync(indexAssembly.Assembly,
                    documentStore,
                    token: cancellationToken);
            }
        }
    }
}
