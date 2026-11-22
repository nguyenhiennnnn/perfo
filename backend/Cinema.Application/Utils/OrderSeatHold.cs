using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class OrderSeatHold
{
    public static IQueryable<ShowtimeSeat> HeldFor(
        this IQueryable<ShowtimeSeat> seats,
        int orderId
    ) => seats.Where(ss => ss.Status == ShowtimeSeatStatus.Held && ss.HeldByOrderId == orderId);
}
