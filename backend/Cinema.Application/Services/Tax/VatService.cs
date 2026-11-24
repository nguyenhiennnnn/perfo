using Cinema.Application.Caching;
using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Tax;

public class VatService(IAppCache cache) : IVatService
{
    public async Task<VatRates> GetRatesAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        return new VatRates(
            settings.TicketVatPercent,
            settings.ComboVatPercent,
            settings.GiftCardVatPercent,
            settings.MembershipVatPercent
        );
    }

    public async Task<IReadOnlyList<OrderLineVat>> SplitOrderLinesAsync(
        Order order,
        CancellationToken cancellationToken
    )
    {
        var rates = await GetRatesAsync(cancellationToken);
        return order
            .Lines.OrderBy(l => l.OrderLineId)
            .Select(line => SplitLine(line, order.Type, rates))
            .ToList();
    }

    public async Task<VatSplit> SplitGiftCardRedemptionAsync(
        decimal amountUsed,
        CancellationToken cancellationToken
    )
    {
        var rates = await GetRatesAsync(cancellationToken);
        return VatCalculator.Split(amountUsed, rates.GiftCardPercent);
    }

    private static OrderLineVat SplitLine(OrderLine line, OrderType orderType, VatRates rates)
    {
        if (line.LineType == OrderLineType.GiftCard)
        {
            var gross = VatCalculator.Split(line.PaidAmount, 0m);
            return new OrderLineVat(
                line.OrderLineId,
                line.LineType,
                gross.GrossAmount,
                gross.NetAmount,
                0m,
                0m,
                true
            );
        }

        var split = VatCalculator.Split(line.PaidAmount, rates.ForLine(line.LineType, orderType));
        return new OrderLineVat(
            line.OrderLineId,
            line.LineType,
            split.GrossAmount,
            split.NetAmount,
            split.TaxAmount,
            split.RatePercent,
            false
        );
    }
}
