using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Invoices;

public record InvoiceProfileDto(
    int InvoiceProfileId,
    string TaxCode,
    string CompanyName,
    string Address,
    string Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record UpsertInvoiceProfileRequest(
    string TaxCode,
    string CompanyName,
    string Address,
    string Email
);

public record DeleteInvoiceProfileResponse(int InvoiceProfileId, bool Deleted);

public record CreateInvoiceRequestRequest(int InvoiceProfileId);

public record CounterInvoiceRequestRequest(
    string TaxCode,
    string CompanyName,
    string Address,
    string Email
);

public record InvoiceRequestDto(
    int InvoiceRequestId,
    int OrderId,
    string OrderCode,
    int? InvoiceProfileId,
    string TaxCode,
    string CompanyName,
    string Address,
    string Email,
    InvoiceRequestStatus Status,
    string? RejectReason,
    string? InvoiceNumber,
    bool HasFile,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    bool NeedsAdjustment,
    DateTime? AdjustmentFlaggedAt,
    decimal AdjustmentRefundAmount,
    DateTime? AdjustmentResolvedAt,
    string? AdjustmentNote
);

public record RejectInvoiceRequest(string Reason);

public record InvoiceRequestListFilter(
    InvoiceRequestStatus? Status,
    string? Search,
    bool? NeedsAdjustment = null
);

public record ResolveInvoiceAdjustmentRequest(string? Note);

public record InvoiceFileInput(Stream Content, string FileName, long Length);

public record InvoiceFileResult(Stream Content, string FileName);
