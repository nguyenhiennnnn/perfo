using Cinema.Application.Interfaces.Pricing;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Pricing;

public sealed class StandardShowtimePricingScopeResolver : IShowtimePricingScopeResolver
{
    public PricingScope Resolve(Showtime showtime) => PricingScope.Standard;
}
