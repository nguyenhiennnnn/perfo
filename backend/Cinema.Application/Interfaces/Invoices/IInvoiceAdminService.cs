using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Invoices;

namespace Cinema.Application.Interfaces.Invoices;

public interface IInvoiceAdminService
{
    Task<OffsetPageResult<InvoiceRequestDto>> GetAsync(
        int page,
        int pageSize,
        InvoiceRequestListFilter filter,
        CancellationToken cancellationToken
    );

    Task<InvoiceRequestDto> IssueAsync(
        int requestId,
        string invoiceNumber,
        InvoiceFileInput? file,
        CancellationToken cancellationToken
    );

    Task<InvoiceRequestDto> ReplaceFileAsync(
        int requestId,
        InvoiceFileInput file,
        CancellationToken cancellationToken
    );

    Task<InvoiceRequestDto> RejectAsync(
        int requestId,
        RejectInvoiceRequest request,
        CancellationToken cancellationToken
    );

    Task<InvoiceRequestDto> ResolveAdjustmentAsync(
        int requestId,
        ResolveInvoiceAdjustmentRequest request,
        CancellationToken cancellationToken
    );

    Task<InvoiceFileResult> DownloadAsync(int requestId, CancellationToken cancellationToken);
}
