using Cinema.Application.Events;
using Cinema.Application.Interfaces;

namespace Cinema.Application.Services.SalesStats;

public class SalesStatsOrderChangedHandler(ISalesStatsService salesStats)
    : IBackgroundEventHandler<OrderConfirmed>,
        IBackgroundEventHandler<OrderCancelled>,
        IBackgroundEventHandler<OrderRefunded>,
        IBackgroundEventHandler<OrderExchanged>
{
    public Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken) =>
        salesStats.RefreshForOrderAsync(businessEvent.OrderId, cancellationToken);

    public Task HandleAsync(OrderCancelled businessEvent, CancellationToken cancellationToken) =>
        salesStats.RefreshForOrderAsync(businessEvent.OrderId, cancellationToken);

    public Task HandleAsync(OrderRefunded businessEvent, CancellationToken cancellationToken) =>
        salesStats.RefreshForOrderAsync(businessEvent.OrderId, cancellationToken);

    public async Task HandleAsync(OrderExchanged businessEvent, CancellationToken cancellationToken)
    {
        await salesStats.RefreshForOrderAsync(businessEvent.ExchangeOrderId, cancellationToken);
        await salesStats.RefreshForOrderAsync(businessEvent.OrderId, cancellationToken);
    }
}
