using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Expenses;

public class ExpenseSettings : TenantScopedEntity
{
    public bool RequireDate { get; set; } = true;
    public bool RequireDescription { get; set; }
    public bool RequireCategory { get; set; }
    public bool RequireProject { get; set; }
}
