using Quartz;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class VehicleEuKontrollRefreshJob(ILogger<VehicleEuKontrollRefreshJob> logger) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Vehicle EU-kontroll refresh not yet implemented");
        return Task.CompletedTask;
    }
}
