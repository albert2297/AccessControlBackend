namespace AccessControl.Application.Services.BackgroundTaskService
{
    public class BackgroundTaskService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<BackgroundTaskService> logger) : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue = taskQueue;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<BackgroundTaskService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Task Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    await workItem(scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing a background work item.");
                }
            }

            _logger.LogInformation("Background Task Service is stopping.");
        }
    }

}
