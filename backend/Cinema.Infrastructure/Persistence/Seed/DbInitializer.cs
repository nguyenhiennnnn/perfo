using Cinema.Infrastructure.Persistence.Seed.Groups;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed;

public static class DbInitializer
{
    private static readonly ISeedGroup[] Groups =
    [
        new CinemaSeedGroup(),
        new RoomCatalogSeedGroup(),
        new RoomSeedGroup(),
        new CatalogSeedGroup(),
        new PersonSeedGroup(),
        new MovieSeedGroup(),
        new MembershipTierSeedGroup(),
        new ComboSeedGroup(),
        new StaffSeedGroup(),
        new CustomerSeedGroup(),
        new VerificationTokenSeedGroup(),
        new ReferralCodeSeedGroup(),
        new ShowtimeSeedGroup(),
        new OrderSeedGroup(),
    ];

    public static async Task SeedAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default,
        bool seedDemoScenarios = true
    )
    {
        var context = new SeedContext(db, logger, seedDemoScenarios);

        foreach (var group in Groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Seeding group {Group}...", group.Name);
            await group.SeedAsync(context, cancellationToken);
            db.ChangeTracker.Clear();
        }
    }
}
