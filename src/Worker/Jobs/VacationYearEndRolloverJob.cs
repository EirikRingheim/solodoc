using Quartz;

namespace Solodoc.Worker.Jobs;

[DisallowConcurrentExecution]
public class VacationYearEndRolloverJob(ILogger<VacationYearEndRolloverJob> logger) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Vacation year-end rollover not yet implemented");
        return Task.CompletedTask;
    }
}
