using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Showtimes;

public class RoomChangeRefundVoidHook(IAppDbContext db) : IOrderCancellationHook
{
    public async Task OnCancelledAsync(
        OrderCancellationContext context,
        CancellationToken cancellationToken
    )
    {
        var orderId = context.Order.OrderId;
        await db
            .RoomChangeRefundDues.Where(d =>
                d.OrderId == orderId && d.Status == RoomChangeRefundStatus.Pending
            )
            .ExecuteUpdateAsync(
                s => s.SetProperty(d => d.Status, RoomChangeRefundStatus.Voided),
                cancellationToken
            );
    }
}
