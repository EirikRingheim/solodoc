using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Expenses;

public class Expense : TenantScopedEntity
{
    public Guid PersonId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? JobId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public ExpenseCategory? Category { get; set; }
    public string? Description { get; set; }
    public string ReceiptFileKey { get; set; } = string.Empty;
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;

    // Approval workflow
    public Guid? ApprovedById { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Payment workflow (accountant)
    public Guid? PaidById { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}
