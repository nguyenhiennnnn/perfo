using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces;

public enum RefundScopeKind
{
    Percent,
    Full,
}

public sealed record RefundScope(
    RefundScopeKind Kind,
    decimal Percent,
    bool DeductFulfilledCombo
)
{
    public static RefundScope ByPercent(decimal percent) =>
        new(RefundScopeKind.Percent, percent, true);

    public static RefundScope Full(bool deductFulfilledCombo) =>
        new(RefundScopeKind.Full, 100m, deductFulfilledCombo);
}

public record RefundPlanItem(int ChargeTransactionId, PaymentMethod Method, decimal Amount);

public record OrderRefundPlan(
    int OrderId,
    decimal TotalAmount,
    decimal FulfilledComboExcluded,
    IReadOnlyList<RefundPlanItem> Items
);

public record OrderRefundReservation(
    int RefundTransactionId,
    int ChargeTransactionId,
    PaymentMethod Method,
    decimal Amount,
    PaymentStatus Status
);

public interface IOrderRefundService
{
    Task<OrderRefundPlan> BuildPlanAsync(
        int orderId,
        RefundScope scope,
        CancellationToken cancellationToken
    );

    Task<OrderRefundPlan> BuildPlanForAmountAsync(
        int orderId,
        decimal amount,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<OrderRefundReservation>> ReserveAsync(
        OrderRefundPlan plan,
        int? performedByUserId,
        string? note,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<PaymentRefundResult>> ExecuteAsync(
        IReadOnlyList<OrderRefundReservation> reservations,
        CancellationToken cancellationToken
    );

    Task ReverseLoyaltyForRefundAsync(
        Order order,
        decimal refundedAmount,
        CancellationToken cancellationToken
    );
}
