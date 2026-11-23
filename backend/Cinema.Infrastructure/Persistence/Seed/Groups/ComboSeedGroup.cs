using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class ComboSeedGroup : ISeedGroup
{
    private sealed record ComboSeed(
        string Name,
        string Description,
        decimal Price,
        string ImageUrl,
        bool IsActive = true
    );

    private static readonly ComboSeed[] Combos =
    [
        new(
            "Combo Solo",
            "1 Bắp ngọt (L) + 1 Nước ngọt (L)",
            89_000m,
            "https://images.unsplash.com/photo-1585647347483-22b66260dfff?w=500"
        ),
        new(
            "Combo Couple",
            "1 Bắp ngọt (L) + 2 Nước ngọt (L)",
            119_000m,
            "https://images.unsplash.com/photo-1572177812156-58036aae439c?w=500"
        ),
        new(
            "Combo Premium",
            "1 Bắp phô mai (L) + 2 Nước ngọt (L) + 1 Snack",
            149_000m,
            "https://images.unsplash.com/photo-1512149673953-1e251807ec7c?w=500"
        ),
        new(
            "Combo Gia Đình",
            "2 Bắp ngọt (L) + 4 Nước ngọt (L) + 2 Snack",
            249_000m,
            "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?w=500"
        ),
        new(
            "Combo Caramel",
            "1 Bắp caramel (L) + 2 Nước ngọt (L)",
            135_000m,
            "https://images.unsplash.com/photo-1578849278619-e73505e9610f?w=500"
        ),
    ];

    public string Name => "combos";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = (
            await db.Combos.Select(c => c.Name).ToListAsync(cancellationToken)
        ).ToHashSet();
        var newCombos = Combos
            .Where(seed => !existing.Contains(seed.Name))
            .Select(seed => new Combo
            {
                Name = seed.Name,
                Description = seed.Description,
                Price = seed.Price,
                ImageUrl = seed.ImageUrl,
                IsActive = seed.IsActive,
            })
            .ToList();
        if (newCombos.Count == 0)
            return;

        db.Combos.AddRange(newCombos);
        await db.SaveChangesAsync(cancellationToken);

        context.Logger.LogInformation("Seeded {Count} combos.", newCombos.Count);
    }
}
