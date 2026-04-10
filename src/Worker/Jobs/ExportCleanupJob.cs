using Quartz;
using Solodoc.Application.Services;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class ExportCleanupJob(IExportService exportService, ILogger<ExportCleanupJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting export cleanup...");
        var count = await exportService.CleanupExpiredExportsAsync(context.CancellationToken);
        logger.LogInformation("Export cleanup complete. {Count} expired exports removed.", count);
    }
}
