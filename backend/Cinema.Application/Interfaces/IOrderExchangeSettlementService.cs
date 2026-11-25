using Cinema.Application.DTOs.Orders;

namespace Cinema.Application.Interfaces;

public interface IOrderExchangeSettlementService
{
    Task<OrderExchangeSettlementResult> SettleAsync(
        int exchangeOrderId,
        int? performedByUserId,
        CancellationToken cancellationToken
    );
}
