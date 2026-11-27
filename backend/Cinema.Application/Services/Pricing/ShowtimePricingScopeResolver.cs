using Cinema.Application.Interfaces.Pricing;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Pricing;

public sealed class ShowtimePricingScopeResolver : IShowtimePricingScopeResolver
{
    public PricingScope Resolve(Showtime showtime, ShowtimeEventSetting? eventSetting) =>
        showtime.Kind == ShowtimeKind.Event
            ? new PricingScope(
                ShowtimePricingMode.Event,
                eventSetting is null
                    ? null
                    : new EventPricing(eventSetting.AdjustmentMode, eventSetting.AdjustmentValue)
            )
            : PricingScope.Standard;
}
