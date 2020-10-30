using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorldDomination.SimpleRavenDb.SampleWebApplication.Domain;

namespace WorldDomination.SimpleRavenDb.SampleWebApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            var ravenDbOptions = Configuration.AddRavenDbConfiguration();
            var ravenDbSetupOptions = new RavenDbSetupOptions
            {
                DocumentCollections = FakeData()
            };

            services.AddSimpleRavenDb(ravenDbOptions, ravenDbSetupOptions);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

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
