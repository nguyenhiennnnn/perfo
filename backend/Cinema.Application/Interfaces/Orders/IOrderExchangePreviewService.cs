using Cinema.Application.DTOs.Orders;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Orders;

public sealed record OrderExchangeCaller(int? CustomerUserId)
{
    public static readonly OrderExchangeCaller Counter = new((int?)null);

    public SalesChannel Channel =>
        CustomerUserId is null ? SalesChannel.Counter : SalesChannel.Online;

    public static OrderExchangeCaller Customer(int userId) => new(userId);
}

public interface IOrderExchangePreviewService
{
    Task<OrderExchangePreviewResponse> PreviewAsync(
        int orderId,
        OrderExchangeCaller caller,
        OrderExchangePreviewQuery query,
        CancellationToken cancellationToken
    );
}
