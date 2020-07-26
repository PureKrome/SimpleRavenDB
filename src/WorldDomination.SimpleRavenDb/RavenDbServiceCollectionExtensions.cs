using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;

namespace WorldDomination.SimpleRavenDb
{
    public static class RavenDbServiceCollectionExtensions
    {
        /// <summary>
        /// Initializes a RavenDb database with some simple default settings.
        /// </summary>
        /// <remarks>The RavenDb instance is also setup as a Singleton in the IoC framework.<br/><b> No data is setup here. Use the SetupRavenDb extension method on an IDocumentStore, to do that.</b></remarks>
        /// <param name="services">Collection of services to setup.</param>
        /// <param name="options">Options required to initialize the database.</param>
        /// <param name="setupOptions">Optional: RavenDb setup options, like seeding data.</param>
        /// <returns>The same collection of services.</returns>
        public static IServiceCollection AddSimpleRavenDb(this IServiceCollection services,
                                                          RavenDbOptions options,
                                                          RavenDbSetupOptions setupOptions = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            const string missingRavenDbConfigurationText = "Missing RavenDb configuration setting: ";

            if (options.ServerUrls?.Any() == false)
            {
                throw new Exception($"{missingRavenDbConfigurationText}{nameof(options.ServerUrls)}");
            }

            if (string.IsNullOrWhiteSpace(options.DatabaseName))
            {
                throw new Exception($"{missingRavenDbConfigurationText}{nameof(options.DatabaseName)}");
            }

            var documentStore = new DocumentStore
            {
                Urls = options.ServerUrls.ToArray(),
                Database = options.DatabaseName
            };

            if (!string.IsNullOrWhiteSpace(options.X509CertificateBase64))
            {
                // REF: https://ayende.com/blog/186881-A/x509-certificates-vs-api-keys-in-ravendb
                documentStore.Certificate = new X509Certificate2(Convert.FromBase64String(options.X509CertificateBase64));
            }


            documentStore.Initialize();

            services.AddSingleton<IDocumentStore>(documentStore);
            services.AddSingleton(setupOptions ?? new RavenDbSetupOptions()); // We need this for our custom Hosted service.

            // We always want to setup RavenDB before the web hosting app.
            services.AddHostedService<RavenDbSetupHostedService>();

            return services;
        }
    }
}
