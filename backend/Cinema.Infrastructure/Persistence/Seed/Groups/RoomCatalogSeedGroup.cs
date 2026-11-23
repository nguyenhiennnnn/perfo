using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class RoomCatalogSeedGroup : ISeedGroup
{
    public const string ScreenType2D = "2D";
    public const string ScreenType3D = "3D";
    public const string ScreenTypeImax = "IMAX";

    public const string SeatTypeStandard = "Standard";
    public const string SeatTypeVip = "VIP";
    public const string SeatTypeCouple = "Couple";

    private sealed record SeatTypeSeed(string Name, decimal PriceMultiplier, bool IsCouple);

    private static readonly string[] ScreenTypes = [ScreenType2D, ScreenType3D, ScreenTypeImax];

    private static readonly SeatTypeSeed[] SeatTypes =
    [
        new(SeatTypeStandard, 1.0m, false),
        new(SeatTypeVip, 1.15m, false),
        new(SeatTypeCouple, 1.25m, true),
    ];

    public string Name => "room-catalog";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existingScreenTypes = (
            await db.ScreenTypes.Select(s => s.Name).ToListAsync(cancellationToken)
        ).ToHashSet();
        var newScreenTypes = ScreenTypes
            .Where(name => !existingScreenTypes.Contains(name))
            .Select(name => new ScreenType { Name = name })
            .ToList();

        var existingSeatTypes = (
            await db.SeatTypes.Select(s => s.Name).ToListAsync(cancellationToken)
        ).ToHashSet();
        var newSeatTypes = SeatTypes
            .Where(seed => !existingSeatTypes.Contains(seed.Name))
            .Select(seed => new SeatType
            {
                Name = seed.Name,
                PriceMultiplier = seed.PriceMultiplier,
                IsCouple = seed.IsCouple,
            })
            .ToList();

        if (newScreenTypes.Count == 0 && newSeatTypes.Count == 0)
            return;

        db.ScreenTypes.AddRange(newScreenTypes);
        db.SeatTypes.AddRange(newSeatTypes);
        await db.SaveChangesAsync(cancellationToken);

        context.Logger.LogInformation(
            "Seeded {ScreenTypes} screen types and {SeatTypes} seat types.",
            newScreenTypes.Count,
            newSeatTypes.Count
        );
    }
}
