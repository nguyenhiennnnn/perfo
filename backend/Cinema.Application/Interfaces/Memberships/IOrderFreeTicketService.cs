using Cinema.Application.DTOs.Memberships;

namespace Cinema.Application.Interfaces.Memberships;

public interface IOrderFreeTicketService
{
    Task<OrderFreeTicketResponse> ApplyAsync(
        int orderId,
        int userId,
        int orderLineId,
        CancellationToken cancellationToken
    );

    Task<OrderFreeTicketResponse> RemoveAsync(
        int orderId,
        int userId,
        int orderLineId,
        CancellationToken cancellationToken
    );
}
