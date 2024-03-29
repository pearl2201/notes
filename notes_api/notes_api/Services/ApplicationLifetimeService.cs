namespace NotesApi.Services
{
    public class ApplicationLifetimeService : IHostedService
    {
        readonly ILogger _logger;
        readonly IHostApplicationLifetime _applicationLifetime;

        public ApplicationLifetimeService(IHostApplicationLifetime applicationLifetime, ILogger<ApplicationLifetimeService> logger)
        {
            _applicationLifetime = applicationLifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // register a callback that sleeps for 30 seconds
            _applicationLifetime.ApplicationStopping.Register(() =>
            {
                _logger.LogInformation("SIGTERM received, waiting for 30 seconds");
                Thread.Sleep(30_000);
                _logger.LogInformation("Termination delay complete, continuing stopping process");
            });
            return Task.CompletedTask;
        }

        // Required to satisfy interface
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
