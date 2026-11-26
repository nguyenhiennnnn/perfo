using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Waitlist;

public record RegisterWaitlistRequest(int SeatCount, bool RequireAdjacent);

public record WaitlistEntryDto(
    int WaitlistEntryId,
    int ShowtimeId,
    int SeatCount,
    bool RequireAdjacent,
    WaitlistStatus Status,
    int? QueuePosition,
    DateTime CreatedAt,
    DateTime? NotifiedAt,
    DateTime? ClosedAt,
    DateTime? PriorityExpiresAt = null,
    IReadOnlyList<int>? ReservedSeatIds = null
);
