using Quartz;
using Solodoc.Application.Services;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class DataAnonymizationJob(IDataMaintenanceService service, ILogger<DataAnonymizationJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting data anonymization...");
        var count = await service.AnonymizeExpiredDataAsync(context.CancellationToken);
        logger.LogInformation("Data anonymization complete. {Count} persons anonymized.", count);
    }
}
