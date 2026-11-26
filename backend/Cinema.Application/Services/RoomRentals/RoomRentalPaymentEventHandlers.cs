using Cinema.Application.Events;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Services.Audit;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalPaymentStatusHandler(IAppDbContext db, IAuditLogger auditLogger)
    : IInTransactionEventHandler<OrderConfirmed>
{
    public async Task HandleAsync(
        OrderConfirmed businessEvent,
        CancellationToken cancellationToken
    )
    {
        var order = await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.OrderId == businessEvent.OrderId
                && o.Type == OrderType.RoomRental
                && o.RoomRentalRequestId != null
                && o.RoomRentalPaymentStage != null
            )
            .Select(o => new
            {
                RequestId = o.RoomRentalRequestId!.Value,
                Stage = o.RoomRentalPaymentStage!.Value,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (order is null)
            return;

        var from = RoomRentalPaymentPlan.RequiredStatus(order.Stage);
        var to =
            order.Stage == RoomRentalPaymentStage.Deposit
                ? RoomRentalRequestStatus.DepositPaid
                : RoomRentalRequestStatus.FullyPaid;

        var request = await db
            .RoomRentalRequests.AsNoTracking()
            .Where(r => r.RoomRentalRequestId == order.RequestId)
            .Select(r => new { r.Status, r.StatusNote })
            .FirstOrDefaultAsync(cancellationToken);
        if (request is null || request.Status != from)
            return;

        var now = DateTime.UtcNow;
        var updated = await db
            .RoomRentalRequests.Where(r =>
                r.RoomRentalRequestId == order.RequestId && r.Status == from
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(r => r.Status, to).SetProperty(r => r.UpdatedAt, now),
                cancellationToken
            );
        if (updated == 0)
            return;

        auditLogger.Record(
            AuditActions.ChangeRoomRentalRequestStatus,
            AuditEntityTypes.RoomRentalRequest,
            order.RequestId.ToString(),
            AuditSnapshots.RoomRentalRequestState(from, request.StatusNote, now),
            AuditSnapshots.RoomRentalRequestState(to, request.StatusNote, now)
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class RoomRentalShowtimeProvisionHandler(
    IAppDbContext db,
    IRoomRentalShowtimeService showtimeService
) : IBackgroundEventHandler<OrderConfirmed>
{
    public async Task HandleAsync(
        OrderConfirmed businessEvent,
        CancellationToken cancellationToken
    )
    {
        var requestId = await db
            .Orders.AsNoTracking()
            .Where(o =>
                o.OrderId == businessEvent.OrderId
                && o.Type == OrderType.RoomRental
                && o.RoomRentalRequestId != null
            )
            .Select(o => o.RoomRentalRequestId)
            .FirstOrDefaultAsync(cancellationToken);
        if (requestId is not { } id)
            return;

        var status = await db
            .RoomRentalRequests.AsNoTracking()
            .Where(r => r.RoomRentalRequestId == id)
            .Select(r => (RoomRentalRequestStatus?)r.Status)
            .FirstOrDefaultAsync(cancellationToken);
        if (status is not (RoomRentalRequestStatus.DepositPaid or RoomRentalRequestStatus.FullyPaid))
            return;

        await showtimeService.EnsureForRequestAsync(id, cancellationToken);
    }
}
