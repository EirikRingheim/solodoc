using Quartz;
using Solodoc.Application.Services;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class ScheduledActivityCheckJob(IScheduledActivityService service, ILogger<ScheduledActivityCheckJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting scheduled activity check...");
        var count = await service.CheckMissedActivitiesAsync(context.CancellationToken);
        logger.LogInformation("Scheduled activity check complete. {Count} notifications created.", count);
    }
}
