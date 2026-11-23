using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class MembershipTierSeedGroup : ISeedGroup
{
    private sealed record TierSeed(string TierName, int MinPoints, decimal DiscountPercent);

    private static readonly TierSeed[] Tiers =
    [
        new("Thành viên", 0, 0m),
        new("Bạc", 500, 5m),
        new("Vàng", 2_000, 10m),
        new("Kim Cương", 5_000, 15m),
    ];

    public string Name => "membership-tiers";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = (
            await db.MembershipTiers.Select(t => t.TierName).ToListAsync(cancellationToken)
        ).ToHashSet();
        var newTiers = Tiers
            .Where(seed => !existing.Contains(seed.TierName))
            .Select(seed => new MembershipTier
            {
                TierName = seed.TierName,
                MinPoints = seed.MinPoints,
                DiscountPercent = seed.DiscountPercent,
            })
            .ToList();
        if (newTiers.Count == 0)
            return;

        db.MembershipTiers.AddRange(newTiers);
        await db.SaveChangesAsync(cancellationToken);

        context.Logger.LogInformation("Seeded {Count} membership tiers.", newTiers.Count);
    }
}
