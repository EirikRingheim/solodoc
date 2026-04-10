using Quartz;
using Solodoc.Application.Services;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class CertificationExpiryCheckJob(ICertificationExpiryService service, ILogger<CertificationExpiryCheckJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting certification expiry check...");
        var count = await service.CheckExpiriesAsync(context.CancellationToken);
        logger.LogInformation("Certification expiry check complete. {Count} notifications created.", count);
    }
}
