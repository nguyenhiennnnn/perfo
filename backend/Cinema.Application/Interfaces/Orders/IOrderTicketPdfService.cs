using Cinema.Application.DTOs.Common;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderTicketPdfService
{
    Task<ExportFileDto> GenerateForCustomerAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    );

    Task<ExportFileDto> GenerateForStaffAsync(int orderId, CancellationToken cancellationToken);
}
