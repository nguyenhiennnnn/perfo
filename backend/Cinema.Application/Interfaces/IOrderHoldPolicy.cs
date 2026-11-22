using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public record OrderHoldPolicyContext(int ShowtimeId, int HolderUserId, int? CreatedByEmployeeId);

public interface IOrderHoldPolicy
{
    SalesChannel Channel { get; }
    void EnsureSalesOpen(Showtime showtime, int stopSellingMinutesBefore);
    Task EnsureNoConflictingPendingAsync(
        OrderHoldPolicyContext context,
        CancellationToken cancellationToken
    );
}
