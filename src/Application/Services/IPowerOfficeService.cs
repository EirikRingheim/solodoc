namespace Solodoc.Application.Services;

public interface IPowerOfficeService
{
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
    Task<List<PowerOfficeEmployee>> GetEmployeesAsync(CancellationToken ct = default);
    Task SyncHoursAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task SyncExpensesAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct = default);
}

public record PowerOfficeEmployee(long Id, string FirstName, string LastName, string? Email);
public record PowerOfficePayItem(Guid Id, string Code, string Name, bool IsActive);
