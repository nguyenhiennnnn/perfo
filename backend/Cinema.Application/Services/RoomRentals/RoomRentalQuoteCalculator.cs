using System.Globalization;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.RoomRentals;

public sealed record RoomRentalQuoteCombo(int ComboId, string Name, decimal UnitPrice, int Quantity);

public sealed record RoomRentalQuoteBuild(
    List<RoomRentalQuoteLineDto> Lines,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal Total
);

public static class RoomRentalQuoteCalculator
{
    public static RoomRentalQuoteBuild Build(
        RoomRentalRateSnapshot rate,
        string roomName,
        int durationMinutes,
        int attendeeCount,
        IReadOnlyList<RoomRentalQuoteCombo> combos,
        IReadOnlyList<RoomRentalQuoteExtraItem> extras,
        decimal? discountAmount,
        decimal? discountPercent,
        string? discountReason
    )
    {
        var lines = new List<RoomRentalQuoteLineDto>();

        var rentalAmount = OrderPricing.RoundToDong(rate.BasePrice);
        lines.Add(
            new RoomRentalQuoteLineDto(
                RoomRentalQuoteLineKind.RoomRental,
                string.Format(Messages.Quote_Line_RoomRental, roomName, durationMinutes),
                1,
                rentalAmount,
                rentalAmount,
                rate.RoomRentalRateId
            )
        );

        var chargeableAttendees = Math.Max(0, attendeeCount - rate.IncludedAttendees);
        if (rate.PerPersonFee > 0m && chargeableAttendees > 0)
        {
            var unit = OrderPricing.RoundToDong(rate.PerPersonFee);
            lines.Add(
                new RoomRentalQuoteLineDto(
                    RoomRentalQuoteLineKind.PerPerson,
                    string.Format(Messages.Quote_Line_PerPerson, chargeableAttendees),
                    chargeableAttendees,
                    unit,
                    OrderPricing.RoundToDong(unit * chargeableAttendees),
                    null
                )
            );
        }

        foreach (var combo in combos)
        {
            var unit = OrderPricing.RoundToDong(combo.UnitPrice);
            lines.Add(
                new RoomRentalQuoteLineDto(
                    RoomRentalQuoteLineKind.Combo,
                    combo.Name,
                    combo.Quantity,
                    unit,
                    OrderPricing.RoundToDong(unit * combo.Quantity),
                    combo.ComboId
                )
            );
        }

        foreach (var extra in extras)
        {
            var unit = OrderPricing.RoundToDong(extra.Amount);
            lines.Add(
                new RoomRentalQuoteLineDto(
                    RoomRentalQuoteLineKind.Extra,
                    extra.Label.Trim(),
                    extra.Quantity,
                    unit,
                    OrderPricing.RoundToDong(unit * extra.Quantity),
                    null
                )
            );
        }

        var subtotal = lines.Sum(l => l.Amount);
        var discount = 0m;
        string? discountLabel = null;

        if (discountPercent is > 0m)
        {
            discount = OrderPricing.RoundToDong(subtotal * discountPercent.Value / 100m);
            discountLabel = string.Format(
                Messages.Quote_Line_DiscountPercent,
                discountPercent.Value.ToString("0.##", CultureInfo.InvariantCulture)
            );
        }
        else if (discountAmount is > 0m)
        {
            discount = OrderPricing.RoundToDong(discountAmount.Value);
            discountLabel = Messages.Quote_Line_Discount;
        }

        if (discount > subtotal)
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteDiscountExceeds);

        if (discount > 0m)
        {
            var reason = discountReason?.Trim();
            var label = string.IsNullOrEmpty(reason)
                ? discountLabel!
                : string.Format(Messages.Quote_Line_WithNote, discountLabel, reason);
            lines.Add(
                new RoomRentalQuoteLineDto(
                    RoomRentalQuoteLineKind.Discount,
                    label,
                    1,
                    -discount,
                    -discount,
                    null
                )
            );
        }

        return new RoomRentalQuoteBuild(lines, subtotal, discount, subtotal - discount);
    }
}
