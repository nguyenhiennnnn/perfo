using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;

namespace Cinema.Application.Utils;

public static class ExchangedOrderLines
{
    public static IQueryable<OrderLine> ExcludingExchangedOut(
        this IQueryable<OrderLine> lines,
        IAppDbContext db
    ) => lines.Where(l => !db.OrderExchanges.Any(e => e.OldOrderLineId == l.OrderLineId));
}
