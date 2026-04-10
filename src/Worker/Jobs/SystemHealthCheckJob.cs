using Quartz;
using Solodoc.Application.Services;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class SystemHealthCheckJob(ISystemHealthService service, ILogger<SystemHealthCheckJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var result = await service.CheckHealthAsync(context.CancellationToken);

        if (result.DatabaseOk)
        {
            logger.LogDebug("System health check passed at {CheckedAt}", result.CheckedAt);
        }
        else
        {
            logger.LogWarning("System health check FAILED at {CheckedAt}. Database: {DatabaseOk}, Storage: {StorageOk}",
                result.CheckedAt, result.DatabaseOk, result.StorageOk);
        }
    }
}
