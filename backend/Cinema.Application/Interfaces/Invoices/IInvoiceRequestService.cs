using Cinema.Application.DTOs.Invoices;

namespace Cinema.Application.Interfaces.Invoices;

public interface IInvoiceRequestService
{
    Task<IReadOnlyList<InvoiceRequestDto>> GetForCustomerOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    );

    Task<InvoiceFileResult> DownloadForCustomerAsync(
        int userId,
        int orderId,
        int requestId,
        CancellationToken cancellationToken
    );

    Task<InvoiceRequestDto> CreateForCustomerAsync(
        int userId,
        int orderId,
        CreateInvoiceRequestRequest request,
        CancellationToken cancellationToken
    );

    Task<InvoiceRequestDto> CreateForCounterAsync(
        int staffUserId,
        int orderId,
        CounterInvoiceRequestRequest request,
        CancellationToken cancellationToken
    );
}
