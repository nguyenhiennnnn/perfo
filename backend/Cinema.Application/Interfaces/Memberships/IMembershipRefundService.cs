using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Interfaces.Orders;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Memberships;

public interface IMembershipRefundService
{
    Task<MembershipRefundResultDto> RefundAsync(
        int subscriptionId,
        CancelActor actor,
        MembershipRefundReason reason,
        string? note,
        CancellationToken cancellationToken
    );
}
