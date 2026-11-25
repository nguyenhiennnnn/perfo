using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Waitlist;

public interface IWaitlistPriorityService
{
    Task<List<int>> ConsumeAsync(
        int userId,
        int showtimeId,
        IReadOnlyCollection<int> requestedSeatIds,
        CancellationToken cancellationToken
    );

    Task<bool> EndAsync(
        int waitlistEntryId,
        WaitlistStatus newStatus,
        bool onlyIfDue,
        CancellationToken cancellationToken
    );
}
