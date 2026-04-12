using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Billing;

public class Invoice : BaseEntity
{
    public Guid TenantId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty; // e.g. "SOL-2026-0001"
    public int Year { get; set; }
    public int Month { get; set; }

    // Customer info snapshot
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerOrgNumber { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }

    // Line items summary
    public int AdminCount { get; set; }
    public int WorkerCount { get; set; }
    public int SubcontractorCount { get; set; }
    public int TemplatePurchases { get; set; }

    // Amounts in kr
    public int BasePriceKr { get; set; }
    public int ExtraUsersKr { get; set; }
    public int SubcontractorsKr { get; set; }
    public int TemplatesKr { get; set; }
    public int DiscountKr { get; set; }
    public string? DiscountReason { get; set; }
    public int TotalExVatKr { get; set; }
    public int VatKr { get; set; } // 25% MVA
    public int TotalIncVatKr { get; set; }

    // Status
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? DueDate { get; set; }

    // EHF
    public string? EhfXml { get; set; }
    public string? EhfFileKey { get; set; } // stored in MinIO
    public string? PdfFileKey { get; set; }

    // Coupon
    public bool IsCouponApplied { get; set; }
    public string? CouponCode { get; set; }
}

public enum InvoiceStatus
{
    Draft,
    Approved,
    Sent,
    Paid,
    Overdue,
    Credited
}
