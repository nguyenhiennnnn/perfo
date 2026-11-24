using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class InvoiceRequest
{
    public int InvoiceRequestId { get; set; }
    public int OrderId { get; set; }
    public int? InvoiceProfileId { get; set; }
    public string TaxCode { get; set; } = null!;
    public string CompanyName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string Email { get; set; } = null!;
    public InvoiceRequestStatus Status { get; set; } = InvoiceRequestStatus.Pending;
    public string? RejectReason { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? FileUrl { get; set; }
    public string? FilePublicId { get; set; }
    public string? FileName { get; set; }
    public bool NeedsAdjustment { get; set; }
    public DateTime? AdjustmentFlaggedAt { get; set; }
    public int? AdjustmentRefundTransactionId { get; set; }
    public decimal AdjustmentRefundAmount { get; set; }
    public DateTime? AdjustmentResolvedAt { get; set; }
    public int? AdjustmentResolvedByUserId { get; set; }
    public string? AdjustmentNote { get; set; }
    public int? RequestedByUserId { get; set; }
    public int? IssuedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public Order Order { get; set; } = null!;
    public InvoiceProfile? InvoiceProfile { get; set; }
    public User? RequestedByUser { get; set; }
    public User? IssuedByUser { get; set; }
}
