namespace Solodoc.Application.Services;

public interface IPdfReportService
{
    Task<byte[]> GenerateDeviationReportAsync(Guid deviationId, CancellationToken ct = default);
    Task<byte[]> GenerateChecklistReportAsync(Guid instanceId, CancellationToken ct = default);
    Task<byte[]> GenerateSjaReportAsync(Guid sjaId, CancellationToken ct = default);
    Task<byte[]> GenerateHoursExportAsync(Guid? projectId, Guid? personId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<byte[]> GenerateProjectSummaryAsync(Guid projectId, CancellationToken ct = default);
    Task<byte[]> GenerateMiniCvAsync(Guid personId, CancellationToken ct = default);
    Task<byte[]> GenerateFullCvAsync(Guid personId, CancellationToken ct = default);
    Task<byte[]> GenerateEquipmentReportAsync(Guid equipmentId, CancellationToken ct = default);
    Task<byte[]> GenerateHmsHandbookAsync(Guid tenantId, CancellationToken ct = default);
}
