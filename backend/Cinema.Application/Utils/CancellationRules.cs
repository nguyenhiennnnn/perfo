using System.Linq.Expressions;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class CancellationRules
{
    private static readonly Expression<Func<Order, bool>> ExchangeExpression = o =>
        o.Type == OrderType.Exchange;

    private static readonly Func<Order, bool> ExchangeCompiled = ExchangeExpression.Compile();

    public static bool IsExchange(Order order) => ExchangeCompiled(order);

    public static IQueryable<Order> ExcludingExchanges(IQueryable<Order> orders) =>
        orders.Where(
            Expression.Lambda<Func<Order, bool>>(
                Expression.Not(ExchangeExpression.Body),
                ExchangeExpression.Parameters
            )
        );
}
