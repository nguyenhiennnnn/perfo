using Cinema.Application.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Cinema.Application.Interfaces.Events;


namespace Cinema.Application.Services.Orders;

public class SupplementCardCheckHandler(IAppDbContext db) : IInTransactionEventHandler<OrderConfirmed>
{
    public async Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken)
    {
        var supplementedLineIds = db
            .OrderLines.Where(l =>
                l.OrderId == businessEvent.OrderId && l.SupplementedLineId != null
            )
            .Select(l => l.SupplementedLineId!.Value);

        await db
            .OrderLines.Where(l =>
                supplementedLineIds.Contains(l.OrderLineId)
                && l.CardCheckStatus == CardCheckStatus.NotPresented
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.CardCheckStatus, CardCheckStatus.Supplemented),
                cancellationToken
            );
    }
}

public class SupplementCardCheckRevertHandler(IAppDbContext db)
    : IInTransactionEventHandler<OrderCancelled>
{
    public async Task HandleAsync(OrderCancelled businessEvent, CancellationToken cancellationToken)
    {
        var supplementedLineIds = db
            .OrderLines.Where(l =>
                l.OrderId == businessEvent.OrderId && l.SupplementedLineId != null
            )
            .Select(l => l.SupplementedLineId!.Value);

        await db
            .OrderLines.Where(l =>
                supplementedLineIds.Contains(l.OrderLineId)
                && l.CardCheckStatus == CardCheckStatus.Supplemented
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(l => l.CardCheckStatus, CardCheckStatus.NotPresented),
                cancellationToken
            );
    }
}
