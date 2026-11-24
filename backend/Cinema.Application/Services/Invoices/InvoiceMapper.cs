using Cinema.Application.DTOs.Invoices;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Invoices;

public static class InvoiceMapper
{
    public static InvoiceProfileDto ToDto(InvoiceProfile p) =>
        new(
            p.InvoiceProfileId,
            p.TaxCode,
            p.CompanyName,
            p.Address,
            p.Email,
            p.CreatedAt,
            p.UpdatedAt
        );

    public static InvoiceRequestDto ToDto(InvoiceRequest r, string orderCode) =>
        new(
            r.InvoiceRequestId,
            r.OrderId,
            orderCode,
            r.InvoiceProfileId,
            r.TaxCode,
            r.CompanyName,
            r.Address,
            r.Email,
            r.Status,
            r.RejectReason,
            r.InvoiceNumber,
            !string.IsNullOrEmpty(r.FileUrl),
            r.CreatedAt,
            r.ProcessedAt,
            r.NeedsAdjustment,
            r.AdjustmentFlaggedAt,
            r.AdjustmentRefundAmount,
            r.AdjustmentResolvedAt,
            r.AdjustmentNote
        );
}
