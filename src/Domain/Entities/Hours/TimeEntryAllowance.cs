using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class TimeEntryAllowance : BaseEntity
{
    public Guid TimeEntryId { get; set; }
    public Guid AllowanceRuleId { get; set; }
    public decimal Hours { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public TimeEntry TimeEntry { get; set; } = null!;
    public AllowanceRule AllowanceRule { get; set; } = null!;
}
