using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace WorldDomination.SimpleRavenDb.SampleApplication
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Need to log stuff...
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
            });

            var logger = loggerFactory.CreateLogger(nameof(Main));

            logger.LogInformation("Starting app.");

            var ravenDbOptions = new RavenDbOptions
            {
                DatabaseName = $"Testing-SimpleRavenDb-{Guid.NewGuid()}",
                ServerUrls = new[] { "http://localhost:5200" }
            };

            var services = new ServiceCollection();

            // This will:
            // - Register the IDocumentStore
            // - Initialize the instance of the DocumentStore
            // - ** NO DATA IS SETUP (that happens later, if you want to) **
            services.AddSimpleRavenDb(ravenDbOptions);

            var serviceProvider = services.BuildServiceProvider();

            logger.LogInformation("Testing to see if we can seed data into RavenDb.");

            // Because we don't have any code that starts 'Hosts', we need to manually
            // pretend we're starting the host.
            using (var documentStore = serviceProvider.GetRequiredService<IDocumentStore>())
            {
                // Can be null - we might not have any setup options, which is totally kewl.
                // (in this sample app, no RavenDbSetupOptions were registered with DI
                //  and so a null instance will be retrieved/returned).
                //  I just wanted to show you the proper pattern. 
                var setupOptions = serviceProvider.GetService<RavenDbSetupOptions>();

                // Now lets setup RavenDb!
                await documentStore.SetupRavenDbAsync(setupOptions, logger, default);
            }

            logger.LogInformation("Finished test app.");
        }
    }
}
