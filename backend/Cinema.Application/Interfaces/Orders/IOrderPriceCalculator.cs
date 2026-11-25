using Cinema.Application.Services.Orders;
using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderPriceCalculator
{
    Task<OrderPriceResult> ApplyAsync(
        Order order,
        OrderPricingOptions options,
        CancellationToken cancellationToken
    );
}
