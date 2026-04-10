using Quartz;
using Solodoc.Application.Services;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class SyncQueueCleanupJob(IDataMaintenanceService service, ILogger<SyncQueueCleanupJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting sync queue cleanup...");
        var count = await service.CleanupSyncQueueAsync(context.CancellationToken);
        logger.LogInformation("Sync queue cleanup complete. {Count} events removed.", count);
    }
}
