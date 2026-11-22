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
}
