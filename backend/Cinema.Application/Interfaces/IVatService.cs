using Cinema.Application.Services.Tax;
using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces;

public interface IVatService
{
    Task<VatRates> GetRatesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderLineVat>> SplitOrderLinesAsync(
        Order order,
        CancellationToken cancellationToken
    );

    Task<VatSplit> SplitGiftCardRedemptionAsync(
        decimal amountUsed,
        CancellationToken cancellationToken
    );
}
