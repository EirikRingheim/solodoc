namespace Solodoc.Application.Services;

public interface IExcelExportService
{
    Task<byte[]> GeneratePayrollExportAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct);
    Task<byte[]> GenerateHoursExportAsync(Guid tenantId, DateOnly from, DateOnly to, Guid? projectId, Guid? personId, CancellationToken ct);
    Task<byte[]> GenerateExpenseExportAsync(Guid tenantId, DateOnly from, DateOnly to, string? status, CancellationToken ct);
    Task<byte[]> GenerateDeviationExportAsync(Guid tenantId, DateOnly from, DateOnly to, string? severity, string? status, Guid? projectId, CancellationToken ct);
    Task<byte[]> GenerateCertificationExportAsync(Guid tenantId, string? type, Guid? personId, string? status, CancellationToken ct);
    Task<byte[]> GenerateEmployeeExportAsync(Guid tenantId, Guid personId, CancellationToken ct);
}
