using Cinema.Application.DTOs.Bookings;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Mapster;

namespace Cinema.Application.MappingConfig;

public class BookingMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<Booking, HoldBookingResponse>()
            .Map(dest => dest.Status, _ => BookingStatus.Pending.ToDtoStatus());
    }
}
