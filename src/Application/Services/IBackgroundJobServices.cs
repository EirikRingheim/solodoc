namespace Solodoc.Application.Services;

public interface ICertificationExpiryService
{
    Task<int> CheckExpiriesAsync(CancellationToken ct);
}

public interface IDeviationReminderService
{
    Task<int> SendOverdueRemindersAsync(CancellationToken ct);
    Task<int> AutoEscalateAsync(CancellationToken ct);
}

public interface IScheduledActivityService
{
    Task<int> CheckMissedActivitiesAsync(CancellationToken ct);
}

public interface IDataMaintenanceService
{
    Task<int> CleanupSyncQueueAsync(CancellationToken ct);
    Task<int> AnonymizeExpiredDataAsync(CancellationToken ct);
    Task<int> ArchiveOldAuditDataAsync(CancellationToken ct);
}

public interface ISystemHealthService
{
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct);
}

public record HealthCheckResult(bool DatabaseOk, bool StorageOk, DateTimeOffset CheckedAt);
