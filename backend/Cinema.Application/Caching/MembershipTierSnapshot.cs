using Cinema.Domain.Entities;

namespace Cinema.Application.Caching;

public sealed record MembershipTierSnapshot(
    int TierId,
    string TierName,
    int MinPoints,
    decimal DiscountPercent
)
{
    public static MembershipTierSnapshot From(MembershipTier t) =>
        new(t.TierId, t.TierName, t.MinPoints, t.DiscountPercent);
}
