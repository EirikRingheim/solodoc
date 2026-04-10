using Quartz;
using Solodoc.Application.Services;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class DeviationAutoEscalationJob(IDeviationReminderService service, ILogger<DeviationAutoEscalationJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting deviation auto-escalation...");
        var count = await service.AutoEscalateAsync(context.CancellationToken);
        logger.LogInformation("Deviation auto-escalation complete. {Count} deviations escalated.", count);
    }
}
