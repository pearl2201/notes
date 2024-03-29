using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NotesApi.Data;

namespace NotesApi.Services
{
    public class ReadinessPublisher : IHealthCheckPublisher
    {
        private readonly ILogger _logger;

        public ReadinessPublisher(ILogger<ReadinessPublisher> logger)
        {
            _logger = logger;
        }

        // The following example is for demonstration purposes only. Health Checks 
        // Middleware already logs health checks results. A real-world readiness 
        // check in a production app might perform a set of more expensive or 
        // time-consuming checks to determine if other resources are responding 
        // properly.
        public Task PublishAsync(HealthReport report,
            CancellationToken cancellationToken)
        {
            if (report.Status == HealthStatus.Healthy)
            {
                //_logger.LogInformation("{Timestamp} Readiness Probe Status: {Result}",
                //    DateTime.UtcNow, report.Status);
            }
            else
            {
                _logger.LogError("{Timestamp} Readiness Probe Status: {Result}",
                    DateTime.UtcNow, report.Status);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }

    public class StartupHostedService : IHostedService, IDisposable
    {
        private readonly int _delaySeconds = 15;
        private readonly ILogger _logger;
        private readonly StartupHostedServiceHealthCheck _startupHostedServiceHealthCheck;
        private readonly IServiceProvider _serviceProvider;

        public StartupHostedService(ILogger<StartupHostedService> logger,
            StartupHostedServiceHealthCheck startupHostedServiceHealthCheck, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _startupHostedServiceHealthCheck = startupHostedServiceHealthCheck;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Startup Background Service is starting.");

            Task.Run(async () =>
            {
                // warmup dbContext
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await dbContext.DataProtectionKeys.AnyAsync();
                }
                await Task.Delay(_delaySeconds * 1000);

                _startupHostedServiceHealthCheck.StartupTaskCompleted = true;
                _startupHostedServiceHealthCheck.FeatureFlagTaskCompleted = true;

                _logger.LogInformation("Startup Background Service has started.");
            });

            return Task.CompletedTask;



        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Startup Background Service is stopping.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {

        }
    }



    public class StartupHostedServiceHealthCheck : IHealthCheck
    {
        private volatile bool _startupTaskCompleted = false;
        private volatile bool _featureFlagTaskCompleted = false;

        public string Name => "slow_dependency_check";

        public bool StartupTaskCompleted
        {
            get => _startupTaskCompleted;
            set => _startupTaskCompleted = value;
        }

        public bool FeatureFlagTaskCompleted
        {
            get => _featureFlagTaskCompleted;
            set => _featureFlagTaskCompleted = value;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (StartupTaskCompleted && FeatureFlagTaskCompleted)
            {
                return Task.FromResult(
                    HealthCheckResult.Healthy("The startup task is finished."));
            }

            return Task.FromResult(
                HealthCheckResult.Unhealthy("The startup task is still running."));
        }
    }

}
