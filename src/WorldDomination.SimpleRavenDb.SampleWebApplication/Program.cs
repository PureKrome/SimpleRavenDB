using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using WorldDomination.SimpleRavenDb.SampleWebApplication.Domain;

namespace WorldDomination.SimpleRavenDb.SampleWebApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)

                // First we create a DB migrations hosted service.
                .ConfigureServices((hostContext, services) =>
                {
                    var ravenDbOptions = hostContext.Configuration.AddRavenDbConfiguration();
                    var ravenDbSetupOptions = new RavenDbSetupOptions
                    {
                        DocumentCollections = FakeData()
                    };

                    services.AddSimpleRavenDb(ravenDbOptions, ravenDbSetupOptions);

                })

                // Finally, we start our normal webhost service.
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static List<IList> FakeData()
        {
            var fakeUsers = new List<User>
            {
                new User {  Name = "Princess Leia" },
                new User {  Name = "Han Solo" }
            };

            var fakeOrders = new List<Order>
            {
                new Order { Price = 1.1m },
                new Order { Price = 2.2m }
            };

            return new List<IList>
            {
                fakeUsers,
                fakeOrders
            };
        }
    }
}
