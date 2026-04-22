namespace Solodoc.Shared.Expenses;

// ── Receipt (Kvittering) ─────────────────────────────

public record ExpenseListItemDto(
    Guid Id, string EmployeeName, Guid PersonId,
    DateOnly Date, decimal Amount, string? Category,
    string? Description, string? ProjectName, Guid? ProjectId,
    string ReceiptFileKey, string Status,
    bool IsApproved, bool IsPaid,
    string? ApprovedByName, string? PaidByName,
    DateTimeOffset CreatedAt);

public record ExpenseDetailDto(
    Guid Id, Guid PersonId, string EmployeeName,
    DateOnly Date, decimal Amount, string? Category,
    string? Description, string? ProjectName, Guid? ProjectId,
    string? JobDescription, Guid? JobId,
    string ReceiptFileKey, string Status,
    string? ApprovedByName, DateTimeOffset? ApprovedAt,
    string? PaidByName, DateTimeOffset? PaidAt,
    string? RejectionReason);

public record CreateExpenseRequest(
    DateOnly? Date, decimal Amount,
    string? Category, string? Description,
    Guid? ProjectId, Guid? JobId,
    string ReceiptFileKey);

public record UpdateExpenseRequest(
    DateOnly? Date, decimal? Amount,
    string? Category, string? Description,
    Guid? ProjectId, Guid? JobId);

public record ApproveExpenseRequest(string? Notes);
public record RejectExpenseRequest(string Reason);

// ── Expense Settings ─────────────────────────────────

public record ExpenseSettingsDto(
    bool RequireDate, bool RequireDescription,
    bool RequireCategory, bool RequireProject);

public record UpdateExpenseSettingsRequest(
    bool RequireDate, bool RequireDescription,
    bool RequireCategory, bool RequireProject);
