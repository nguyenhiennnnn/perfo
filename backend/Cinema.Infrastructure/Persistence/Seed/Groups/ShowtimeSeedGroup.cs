using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class ShowtimeSeedGroup : ISeedGroup
{
    public string Name => "showtimes";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var scenariosPending =
            context.SeedDemoScenarios && !await context.Db.Showtimes.AnyAsync(cancellationToken);

        if (!scenariosPending)
        {
            await ShowtimeSeeder.TopUpFutureAsync(
                context.Db,
                context.Logger,
                DateTime.UtcNow,
                cancellationToken
            );
            return;
        }

        await SeedScenariosAsync(context.Db, context.Logger, cancellationToken);
    }

    private static async Task SeedScenariosAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            logger.LogInformation("Seeding demo scenarios (showtimes)...");

            await ShowtimeSeeder.CreateSpecialsAsync(db, logger, now, cancellationToken);
            await ShowtimeSeeder.TopUpFutureAsync(db, logger, now, cancellationToken);
            await ShowtimeSeeder.SeedPastAsync(db, logger, now, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Demo scenarios seeded successfully.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            logger.LogError(
                ex,
                "Seed demo scenarios thất bại — đã rollback. Hệ thống vẫn chạy với dữ liệu tĩnh + suất chiếu cơ bản."
            );

            await ShowtimeSeeder.TopUpFutureAsync(db, logger, DateTime.UtcNow, cancellationToken);
        }
    }
}
