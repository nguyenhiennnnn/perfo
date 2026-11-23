using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class TicketTypeSeedGroup : ISeedGroup
{
    private static readonly string[] ChildAgeRatingCodes = ["P", "K"];

    private sealed record TicketTypeSeed(
        string Name,
        decimal PriceMultiplier,
        bool RequiresCardVerification,
        bool ChildAgeRatingsOnly
    );

    private static readonly TicketTypeSeed[] TicketTypes =
    [
        new("Người lớn", 1m, false, false),
        new("Học sinh - sinh viên", 0.8m, true, false),
        new("Trẻ em", 0.7m, false, true),
        new("Người cao tuổi", 0.7m, true, false),
    ];

    public string Name => "ticket-types";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = (
            await db
                .TicketTypes.IgnoreQueryFilters()
                .Select(t => t.Name)
                .ToListAsync(cancellationToken)
        ).ToHashSet();
        var missing = TicketTypes.Where(seed => !existing.Contains(seed.Name)).ToList();
        if (missing.Count == 0)
            return;

        var childAgeRatingIds = await db
            .AgeRatings.Where(a => ChildAgeRatingCodes.Contains(a.Code))
            .Select(a => a.AgeRatingId)
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        db.TicketTypes.AddRange(
            missing.Select(seed => new TicketType
            {
                Name = seed.Name,
                PriceMultiplier = seed.PriceMultiplier,
                RequiresCardVerification = seed.RequiresCardVerification,
                AllowedAgeRatingIds = seed.ChildAgeRatingsOnly ? [.. childAgeRatingIds] : [],
                IsActive = true,
                CreatedAt = now,
            })
        );
        await db.SaveChangesAsync(cancellationToken);

        context.Logger.LogInformation("Seeded {Count} ticket types.", missing.Count);
    }
}
