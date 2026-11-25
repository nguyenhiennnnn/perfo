using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Orders;

namespace Cinema.Application.Services.Orders;

public class OrderLoyaltyEarnHandler(IOrderLoyaltyService loyalty)
    : IInTransactionEventHandler<OrderConfirmed>
{
    public Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken) =>
        loyalty.AwardForOrderAsync(businessEvent.OrderId, cancellationToken);
}

public class OrderLoyaltyRevokeHandler(IOrderLoyaltyService loyalty)
    : IInTransactionEventHandler<OrderRefunded>,
        IInTransactionEventHandler<OrderCancelled>
{
    public Task HandleAsync(OrderRefunded businessEvent, CancellationToken cancellationToken) =>
        loyalty.RevokeForOrderAsync(businessEvent.OrderId, cancellationToken);

    public Task HandleAsync(OrderCancelled businessEvent, CancellationToken cancellationToken) =>
        loyalty.RevokeForOrderAsync(businessEvent.OrderId, cancellationToken);
}
