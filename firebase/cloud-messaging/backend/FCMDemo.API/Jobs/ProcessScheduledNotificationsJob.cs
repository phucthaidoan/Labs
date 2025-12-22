using FCMDemo.API.Services;
using Quartz;

namespace FCMDemo.API.Jobs;

/// <summary>
/// Background job that processes scheduled notifications
/// Runs every minute to check for and send notifications that are due
/// </summary>
[DisallowConcurrentExecution]
public class ProcessScheduledNotificationsJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessScheduledNotificationsJob> _logger;

    public ProcessScheduledNotificationsJob(
        IServiceProvider serviceProvider,
        ILogger<ProcessScheduledNotificationsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("ProcessScheduledNotificationsJob started at {Time}", DateTime.UtcNow);

        try
        {
            // Create a scope to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var schedulingService = scope.ServiceProvider.GetRequiredService<INotificationSchedulingService>();

            // Process scheduled notifications
            var processedCount = await schedulingService.ProcessScheduledNotificationsAsync();

            if (processedCount > 0)
            {
                _logger.LogInformation(
                    "ProcessScheduledNotificationsJob completed: Processed {Count} notifications",
                    processedCount);
            }
            else
            {
                _logger.LogDebug("ProcessScheduledNotificationsJob completed: No notifications to process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessScheduledNotificationsJob");
            // Don't rethrow - we want the job to continue running
        }
    }
}
