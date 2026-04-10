namespace Solodoc.Application.Services;

public interface IExportService
{
    Task<Guid> CreateProjectExportAsync(Guid projectId, string outputMode, string? photoOption, Guid requestedById, Guid tenantId, CancellationToken ct = default);
    Task<Guid> CreateEmployeeExportAsync(Guid personId, string outputMode, Guid requestedById, Guid tenantId, CancellationToken ct = default);
    Task<Guid> CreateCustomExportAsync(string outputMode, string? photoOption, string selectionJson, Guid requestedById, Guid tenantId, CancellationToken ct = default);
    Task ProcessExportAsync(Guid exportJobId, CancellationToken ct = default);
    Task<int> CleanupExpiredExportsAsync(CancellationToken ct = default);
}
