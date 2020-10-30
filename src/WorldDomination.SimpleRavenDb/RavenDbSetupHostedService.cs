using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace WorldDomination.SimpleRavenDb
{
    /*
     * REFERENCE FOR HOSTED SERVICES: https://andrewlock.net/running-async-tasks-on-app-startup-in-asp-net-core-3/
     */
    public class RavenDbSetupHostedService : IHostedService
    {
        private readonly RavenDbSetupOptions _setupOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RavenDbSetupHostedService> _logger;

        public RavenDbSetupHostedService(RavenDbSetupOptions setupOptions,
            IServiceProvider serviceProvider,
            ILogger<RavenDbSetupHostedService> logger)
        {
            _setupOptions = setupOptions ?? throw new ArgumentNullException(nameof(setupOptions));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting to setup RavenDb...");

            using (var scope = _serviceProvider.CreateScope())
            {
                var documentStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

                await documentStore.SetupRavenDbAsync(_setupOptions, _logger, cancellationToken);
            }

            _logger.LogInformation(" - All finished setting up the database (⌐■_■)");

            return;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
