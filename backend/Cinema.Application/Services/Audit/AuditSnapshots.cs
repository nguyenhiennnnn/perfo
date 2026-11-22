using System.Text.Json.Nodes;
using Cinema.Application.Caching;
using Cinema.Domain.Entities;

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
}
