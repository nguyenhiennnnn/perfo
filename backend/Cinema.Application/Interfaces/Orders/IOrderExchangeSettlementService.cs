using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces.Orders;

public interface IOrderExchangeSettlementService
{
    Task<OrderExchangeSettlementResult> SettleAsync(
        int exchangeOrderId,
        int? performedByUserId,
        CancellationToken cancellationToken
    );
}
