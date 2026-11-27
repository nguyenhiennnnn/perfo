using Cinema.Application.Services.Pricing;
using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Pricing;

public interface IShowtimePricingScopeResolver
{
    PricingScope Resolve(Showtime showtime, ShowtimeEventSetting? eventSetting);
}
