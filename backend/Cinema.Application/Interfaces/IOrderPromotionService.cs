using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces;

public sealed record OrderPromotionCaller(int? CustomerUserId)
{
    public static readonly OrderPromotionCaller Counter = new((int?)null);

    public static OrderPromotionCaller Customer(int userId) => new(userId);
}

public interface IOrderPromotionService
{
    Task<OrderPromotionResponse> ApplyAsync(
        int orderId,
        OrderPromotionCaller caller,
        string code,
        CancellationToken cancellationToken
    );

    Task<OrderPromotionResponse> RemoveAsync(
        int orderId,
        OrderPromotionCaller caller,
        CancellationToken cancellationToken
    );
}
