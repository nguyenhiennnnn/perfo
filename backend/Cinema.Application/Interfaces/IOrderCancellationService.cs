using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public enum CancelActorKind
{
    Customer,
    Staff,
    Admin,
}

public sealed record CancelActor(CancelActorKind Kind, int UserId)
{
    public bool IsCustomer => Kind == CancelActorKind.Customer;

    public CancelledBy CancelledBy =>
        Kind switch
        {
            CancelActorKind.Customer => CancelledBy.Customer,
            CancelActorKind.Staff => CancelledBy.Staff,
            _ => CancelledBy.Admin,
        };
}

public sealed record OrderCancellationRefund(
    int RefundTransactionId,
    PaymentMethod Method,
    decimal Amount,
    PaymentStatus Status
);

public sealed record OrderCancellationResult(
    int OrderId,
    OrderStatus Status,
    decimal TotalRefunded,
    IReadOnlyList<OrderCancellationRefund> Refunds
);

public interface IOrderCancellationService
{
    Task<OrderCancellationResult> CancelAsync(
        int orderId,
        CancelActor actor,
        CancelReason reason,
        CancellationToken cancellationToken
    );
}
