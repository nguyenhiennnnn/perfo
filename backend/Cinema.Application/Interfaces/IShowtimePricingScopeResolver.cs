using Cinema.Application.Services.Pricing;
using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces;

public interface IShowtimePricingScopeResolver
{
    PricingScope Resolve(Showtime showtime);
}
