using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class SellableSeats
{
    public static IQueryable<ShowtimeSeat> WhereSellable(this IQueryable<ShowtimeSeat> seats) =>
        seats.Where(ss => ss.Status != ShowtimeSeatStatus.Blocked);
}
