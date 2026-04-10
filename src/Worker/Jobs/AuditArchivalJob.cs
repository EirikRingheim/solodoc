using Quartz;
using Solodoc.Application.Services;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class AuditArchivalJob(IDataMaintenanceService service, ILogger<AuditArchivalJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting audit archival...");
        var count = await service.ArchiveOldAuditDataAsync(context.CancellationToken);
        logger.LogInformation("Audit archival complete. {Count} events archived.", count);
    }
}
