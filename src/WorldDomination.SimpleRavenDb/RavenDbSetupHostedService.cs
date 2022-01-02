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

        public RavenDbSetupHostedService(
            RavenDbSetupOptions setupOptions,
            IServiceProvider serviceProvider,
            ILogger<RavenDbSetupHostedService> logger)
        {
            _setupOptions = setupOptions;
            _serviceProvider = serviceProvider;
            _logger = logger;
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
