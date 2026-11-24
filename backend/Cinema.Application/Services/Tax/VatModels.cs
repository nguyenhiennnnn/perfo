using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Tax;

public readonly record struct VatSplit(decimal GrossAmount, decimal NetAmount, decimal TaxAmount, decimal RatePercent);

public sealed record VatRates(
    decimal TicketPercent,
    decimal ComboPercent,
    decimal GiftCardPercent,
    decimal MembershipPercent
)
{
    public decimal ForLine(OrderLineType lineType, OrderType orderType) =>
        lineType switch
        {
            OrderLineType.Ticket => TicketPercent,
            OrderLineType.Combo => ComboPercent,
            OrderLineType.GiftCard => GiftCardPercent,
            OrderLineType.Fee when orderType == OrderType.Membership => MembershipPercent,
            _ => 0m,
        };
}

public sealed record OrderLineVat(
    int OrderLineId,
    OrderLineType LineType,
    decimal GrossAmount,
    decimal NetAmount,
    decimal TaxAmount,
    decimal RatePercent,
    bool IsTaxDeferred
);
