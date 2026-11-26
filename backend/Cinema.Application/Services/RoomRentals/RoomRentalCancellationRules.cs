using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.RoomRentals;

public static class RoomRentalCancellationRules
{
    public static readonly RoomRentalRequestStatus[] CancellableStatuses =
    [
        RoomRentalRequestStatus.Accepted,
        RoomRentalRequestStatus.DepositPaid,
        RoomRentalRequestStatus.FullyPaid,
    ];

    public static bool IsPaymentStage(RoomRentalRequestStatus status) =>
        CancellableStatuses.Contains(status);

    public static IQueryable<RoomRentalRequest> OverdueRequests(IAppDbContext db, DateTime now) =>
        db.RoomRentalRequests.Where(r =>
            r.Status == RoomRentalRequestStatus.DepositPaid
            && db.RoomRentalQuotes.Any(q =>
                q.RoomRentalRequestId == r.RoomRentalRequestId
                && q.SentAt != null
                && q.BalanceDueAt <= now
                && q.Total > q.DepositAmount
                && !db.RoomRentalQuotes.Any(n =>
                    n.RoomRentalRequestId == r.RoomRentalRequestId
                    && n.SentAt != null
                    && n.VersionNumber > q.VersionNumber
                )
            )
        );

    public static int DepositRefundPercent(
        RoomRentalCancelKind kind,
        RoomRentalQuote quote,
        DateTime now
    ) =>
        kind switch
        {
            RoomRentalCancelKind.Venue => 100,
            RoomRentalCancelKind.PaymentOverdue => 0,
            _ => RoomRentalCancellationPolicy.RefundPercentFor(
                RoomRentalCancellationPolicy.Parse(quote.CancellationPolicyJson),
                (quote.StartAt - now).TotalHours
            ),
        };

    public static CancelReason ReasonOf(RoomRentalCancelKind kind) =>
        kind switch
        {
            RoomRentalCancelKind.Venue => CancelReason.ShowtimeCancelled,
            RoomRentalCancelKind.PaymentOverdue => CancelReason.Other,
            _ => CancelReason.CustomerChangedMind,
        };

    public static CancelledBy CancelledByOf(RoomRentalCancelKind kind) =>
        kind == RoomRentalCancelKind.PaymentOverdue ? CancelledBy.System : CancelledBy.Admin;
}
