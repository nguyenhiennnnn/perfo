using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;

namespace Cinema.Application.Utils;

public static class OrderCancellationRules
{
    public static bool IsReasonAllowed(CancelActorKind kind, CancelReason reason) =>
        kind == CancelActorKind.Customer
            ? reason
                is CancelReason.CustomerChangedMind
                    or CancelReason.DuplicateOrder
                    or CancelReason.RoomClosed
                    or CancelReason.Other
            : reason
                is CancelReason.CustomerChangedMind
                    or CancelReason.DuplicateOrder
                    or CancelReason.PaymentError
                    or CancelReason.RoomClosed
                    or CancelReason.Other;

    public static CancelActorKind KindForRole(string? role) =>
        role switch
        {
            Roles.Admin => CancelActorKind.Admin,
            Roles.Employee => CancelActorKind.Staff,
            _ => CancelActorKind.Customer,
        };

    public static bool TryParseReason(string? value, out CancelReason reason)
    {
        var name = Enum.GetNames<CancelReason>()
            .FirstOrDefault(n => string.Equals(n, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            reason = default;
            return false;
        }

        reason = Enum.Parse<CancelReason>(name);
        return true;
    }

    public static CancelReason ParseReason(string? value)
    {
        if (TryParseReason(value, out var reason))
            return reason;

        throw new ValidationAppException(
            Messages.Invalid_CancelReasonNotAllowed,
            new Dictionary<string, string[]> { ["reason"] = [Messages.Invalid_CancelReasonNotAllowed] }
        );
    }

    public static void EnsureReasonAllowed(CancelActorKind kind, CancelReason reason)
    {
        if (IsReasonAllowed(kind, reason))
            return;

        throw new ValidationAppException(
            Messages.Invalid_CancelReasonNotAllowed,
            new Dictionary<string, string[]> { ["reason"] = [Messages.Invalid_CancelReasonNotAllowed] }
        );
    }

    public static void EnsureRefundEligible(
        decimal refundPercent,
        int deadlineHours,
        DateTime? showtimeStart,
        DateTime now
    )
    {
        if (refundPercent <= 0m)
            throw new BusinessRuleException(Messages.Rule_RefundNotSupported);

        if (showtimeStart is { } start && now > start.AddHours(-deadlineHours))
            throw new BusinessRuleException(
                string.Format(Messages.Rule_RefundDeadlineExceeded, deadlineHours)
            );
    }

    public static void EnsureRoomChangeCancellable(
        IReadOnlyList<(ShowtimeStatus Status, DateTime StartTime)> changedShowtimes,
        DateTime now
    )
    {
        if (
            changedShowtimes.Count == 0
            || changedShowtimes.Any(s => s.Status != ShowtimeStatus.Scheduled)
        )
            throw new BusinessRuleException(Messages.Rule_RoomChangeCancelNotAllowed);

        if (changedShowtimes.Any(s => s.StartTime <= now))
            throw new BusinessRuleException(Messages.Rule_RoomChangeCancelShowtimeStarted);
    }

    public static bool IsCustomerRefundableMethod(PaymentMethod method) =>
        method.IsGatewayMethod() || method == PaymentMethod.GiftCard;
}
