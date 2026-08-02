using OneeProject.Services.Services;

namespace OneeProjectFEAPI.BackgroundServices
{
    public class OfferTimeoutService(IServiceScopeFactory scopeFactory, ILogger<OfferTimeoutService> logger)
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<OfferTimeoutService> _logger = logger;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FEAPI OfferTimeoutService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
                    await jobService.ProcessExpiredOffersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing expired job offers.");
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }
}
