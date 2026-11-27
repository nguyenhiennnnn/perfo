using System.Globalization;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Pricing;

public sealed record PriceBreakdown(
    decimal ReferencePrice,
    decimal DynamicAdjustment,
    decimal ListPrice,
    IReadOnlyList<PriceAdjustmentDto> Adjustments
);

public static class PriceCalculator
{
    public static PriceBreakdown Calculate(
        decimal basePrice,
        decimal seatMultiplier,
        decimal ticketTypeMultiplier,
        IReadOnlyList<AppliedRule> rules,
        PricingParameters parameters,
        EventPricing? eventPricing = null
    )
    {
        if (eventPricing is not null)
            return CalculateEvent(
                basePrice,
                seatMultiplier,
                ticketTypeMultiplier,
                eventPricing,
                parameters
            );

        var reference = basePrice * seatMultiplier;
        var maxIncrease = reference * parameters.MaxIncreasePercent / 100m;
        var maxDecrease = reference * parameters.MaxDecreasePercent / 100m;

        var adjustments = new List<PriceAdjustmentDto>(rules.Count + 2);
        var rawAdjustment = 0m;
        foreach (var applied in rules)
        {
            var amount =
                applied.Rule.AdjustmentMode == PricingAdjustmentMode.Multiplier
                    ? reference * (applied.Value - 1m)
                    : applied.Value;
            rawAdjustment += amount;
            adjustments.Add(
                new PriceAdjustmentDto(
                    PriceAdjustmentKinds.Rule,
                    applied.Rule.PricingRuleId,
                    applied.Rule.Name,
                    applied.Rule.RuleType.ToString(),
                    applied.Rule.AdjustmentMode.ToString(),
                    applied.Value,
                    OrderPricing.RoundToDong(amount),
                    applied.Reason
                )
            );
        }

        var adjustment = Math.Clamp(rawAdjustment, -maxDecrease, maxIncrease);
        if (rawAdjustment > maxIncrease)
            adjustments.Add(
                new PriceAdjustmentDto(
                    PriceAdjustmentKinds.DynamicCap,
                    null,
                    null,
                    null,
                    null,
                    parameters.MaxIncreasePercent,
                    OrderPricing.RoundToDong(maxIncrease - rawAdjustment),
                    string.Format(
                        Messages.Pricing_Limit_MaxIncrease,
                        parameters.MaxIncreasePercent.ToString("0.##", CultureInfo.InvariantCulture)
                    )
                )
            );
        else if (rawAdjustment < -maxDecrease)
            adjustments.Add(
                new PriceAdjustmentDto(
                    PriceAdjustmentKinds.DynamicFloor,
                    null,
                    null,
                    null,
                    null,
                    parameters.MaxDecreasePercent,
                    OrderPricing.RoundToDong(-maxDecrease - rawAdjustment),
                    string.Format(
                        Messages.Pricing_Limit_MaxDecrease,
                        parameters.MaxDecreasePercent.ToString("0.##", CultureInfo.InvariantCulture)
                    )
                )
            );

        var step = parameters.RoundingStep > 0m ? parameters.RoundingStep : 1m;
        var upper = (reference + maxIncrease) * ticketTypeMultiplier;
        var lower = (reference - maxDecrease) * ticketTypeMultiplier;

        var price = RoundToStep((reference + adjustment) * ticketTypeMultiplier, step);
        if (price > upper)
            price = FloorToStep(upper, step);
        else if (price < lower)
            price = CeilToStep(lower, step);

        price = ApplyMinPrice(price, step, parameters, adjustments);

        return new PriceBreakdown(
            OrderPricing.RoundToDong(reference),
            OrderPricing.RoundToDong(adjustment),
            price,
            adjustments
        );
    }

    private static PriceBreakdown CalculateEvent(
        decimal basePrice,
        decimal seatMultiplier,
        decimal ticketTypeMultiplier,
        EventPricing eventPricing,
        PricingParameters parameters
    )
    {
        var reference = basePrice * seatMultiplier;
        var step = parameters.RoundingStep > 0m ? parameters.RoundingStep : 1m;
        var adjustments = new List<PriceAdjustmentDto>(2);
        decimal price;
        if (eventPricing.Mode == PricingAdjustmentMode.Multiplier)
        {
            price = RoundToStep(reference * eventPricing.Value * ticketTypeMultiplier, step);
            adjustments.Add(
                new PriceAdjustmentDto(
                    PriceAdjustmentKinds.EventPricing,
                    null,
                    Messages.Pricing_Event_RuleName,
                    null,
                    nameof(PricingAdjustmentMode.Multiplier),
                    eventPricing.Value,
                    OrderPricing.RoundToDong(reference * (eventPricing.Value - 1m)),
                    string.Format(
                        Messages.Pricing_Reason_EventMultiplier,
                        eventPricing.Value.ToString("0.####", CultureInfo.InvariantCulture)
                    )
                )
            );
        }
        else
        {
            var surcharge = OrderPricing.RoundToDong(eventPricing.Value);
            price = RoundToStep(reference * ticketTypeMultiplier, step) + surcharge;
            adjustments.Add(
                new PriceAdjustmentDto(
                    PriceAdjustmentKinds.EventPricing,
                    null,
                    Messages.Pricing_Event_RuleName,
                    null,
                    nameof(PricingAdjustmentMode.Amount),
                    eventPricing.Value,
                    surcharge,
                    string.Format(
                        CultureInfo.GetCultureInfo(RegionalDefaults.Locale),
                        Messages.Pricing_Reason_EventSurcharge,
                        surcharge
                    )
                )
            );
        }

        price = ApplyMinPrice(price, step, parameters, adjustments);

        return new PriceBreakdown(OrderPricing.RoundToDong(reference), 0m, price, adjustments);
    }

    private static decimal ApplyMinPrice(
        decimal price,
        decimal step,
        PricingParameters parameters,
        List<PriceAdjustmentDto> adjustments
    )
    {
        if (price >= parameters.MinTicketPrice)
            return price;

        var minPrice = CeilToStep(parameters.MinTicketPrice, step);
        adjustments.Add(
            new PriceAdjustmentDto(
                PriceAdjustmentKinds.MinPrice,
                null,
                null,
                null,
                null,
                parameters.MinTicketPrice,
                OrderPricing.RoundToDong(minPrice - price),
                string.Format(
                    CultureInfo.GetCultureInfo(RegionalDefaults.Locale),
                    Messages.Pricing_Limit_MinPrice,
                    parameters.MinTicketPrice
                )
            )
        );
        return minPrice;
    }

    private static decimal RoundToStep(decimal value, decimal step) =>
        Math.Round(value / step, 0, MidpointRounding.AwayFromZero) * step;

    private static decimal FloorToStep(decimal value, decimal step) =>
        Math.Floor(value / step) * step;

    private static decimal CeilToStep(decimal value, decimal step) =>
        Math.Ceiling(value / step) * step;
}
