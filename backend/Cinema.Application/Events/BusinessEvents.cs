using Cinema.Domain.Enums;

namespace Cinema.Application.Events;

public sealed record OrderConfirmed(int OrderId, DateTime OccurredAt) : IBusinessEvent;

public sealed record OrderCancelled(
    int OrderId,
    CancelReason? Reason,
    CancelledBy? CancelledBy,
    DateTime OccurredAt
) : IBusinessEvent;

public sealed record OrderRefunded(
    int OrderId,
    int RefundTransactionId,
    decimal Amount,
    DateTime OccurredAt
) : IBusinessEvent;

public sealed record ShowtimeEnded(int ShowtimeId, DateTime OccurredAt) : IBusinessEvent;

public sealed record ShowtimeRoomChanged(
    int ShowtimeRoomChangeId,
    int ShowtimeId,
    int FromRoomId,
    int ToRoomId,
    DateTime OccurredAt
) : IBusinessEvent;

public sealed record TicketCheckedIn(
    int OrderId,
    int OrderLineId,
    int ShowtimeId,
    DateTime OccurredAt
) : IBusinessEvent;
