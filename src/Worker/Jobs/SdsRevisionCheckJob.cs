using Quartz;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class SdsRevisionCheckJob(ILogger<SdsRevisionCheckJob> logger) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("SDS revision check not yet implemented");
        return Task.CompletedTask;
    }
}
