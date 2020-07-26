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
            var services = new ServiceCollection();
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
            });

            var logger = loggerFactory.CreateLogger(nameof(Main));

            logger.LogInformation("Starting app.");

            var fakeUsers = new List<FakeUser>
            {
                new FakeUser
                {
                    Name = "Tester"
                }
            };
            var fakeData = new List<IEnumerable>
            {
                fakeUsers
            };

            var ravenDbOptions = new RavenDbOptions
            {
                DatabaseName = "Testing-SimpleRavenDb",
                ServerUrls = new[] { "http://localhost:5200" }
            };

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
                var setupOptions = serviceProvider.GetService<RavenDbSetupOptions>();

                // Now lets setup RavenDb!
                await documentStore.SetupRavenDbAsync(setupOptions, logger, default);
            }

            logger.LogInformation("Finished test app.");
        }
    }
}
