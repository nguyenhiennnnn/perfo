using System.Text.Json.Nodes;
using Cinema.Application.Caching;
using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Audit;

public static class AuditSnapshots
{
    public static JsonObject Settings(CinemaSettings settings, bool defaultPasswordChanged)
    {
        var snapshot = AuditJson.ToObject(CinemaSettingsSnapshot.From(settings));
        if (defaultPasswordChanged)
            snapshot["defaultEmployeePasswordChanged"] = true;
        return snapshot;
    }

    public static MembershipTierSnapshot Tier(MembershipTier tier) =>
        MembershipTierSnapshot.From(tier);

    public static JsonObject OrderBeforeCancel(OrderStatus status) =>
        AuditJson.ToObject(new { status });

    public static JsonObject OrderAfterCancel(
        CancelActor actor,
        CancelReason reason,
        decimal refundAmount,
        decimal manualRefundAmount
    ) =>
        AuditJson.ToObject(
            new
            {
                status = OrderStatus.Cancelled,
                reason,
                cancelledBy = actor.CancelledBy,
                actorKind = actor.Kind,
                actorUserId = actor.UserId,
                refundAmount,
                manualRefundAmount,
            }
        );

    public static JsonObject ShowtimeBeforeCancel() =>
        AuditJson.ToObject(new { status = ShowtimeStatus.Scheduled });

    public static JsonObject ShowtimeAfterCancel(
        int ordersCancelled,
        int autoRefundOrders,
        int manualRefundOrders,
        decimal manualRefundAmount
    ) =>
        AuditJson.ToObject(
            new
            {
                status = ShowtimeStatus.Cancelled,
                ordersCancelled,
                autoRefundOrders,
                manualRefundOrders,
                manualRefundAmount,
            }
        );

    public static JsonObject CashRefundBeforeConfirm() =>
        AuditJson.ToObject(new { status = PaymentStatus.Pending, needsManualRefund = true });

    public static JsonObject CashRefundAfterConfirm(PaymentTransaction refund, int confirmedByUserId) =>
        AuditJson.ToObject(
            new
            {
                status = PaymentStatus.Success,
                needsManualRefund = false,
                orderId = refund.OrderId,
                amount = refund.Amount,
                confirmedByUserId,
            }
        );
}
