using Quartz;
using Solodoc.Application.Services;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class OverdueDeviationReminderJob(IDeviationReminderService service, ILogger<OverdueDeviationReminderJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting overdue deviation reminder check...");
        var count = await service.SendOverdueRemindersAsync(context.CancellationToken);
        logger.LogInformation("Overdue deviation reminder check complete. {Count} notifications created.", count);
    }
}
