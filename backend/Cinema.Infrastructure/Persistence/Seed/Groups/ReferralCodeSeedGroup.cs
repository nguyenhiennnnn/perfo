using Cinema.Application.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class ReferralCodeSeedGroup : ISeedGroup
{
    public string Name => "referral-codes";

    public Task SeedAsync(SeedContext context, CancellationToken cancellationToken) =>
        BackfillReferralCodesAsync(context.Db, context.Logger, cancellationToken);

    private static async Task BackfillReferralCodesAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var profiles = await db
            .CustomerProfiles.Where(p => p.ReferralCode == null)
            .ToListAsync(cancellationToken);
        if (profiles.Count == 0)
            return;

        var used = (
            await db
                .CustomerProfiles.Where(p => p.ReferralCode != null)
                .Select(p => p.ReferralCode!)
                .ToListAsync(cancellationToken)
        ).ToHashSet();

        foreach (var profile in profiles)
        {
            string code;
            do
            {
                code = ReferralCodeGenerator.Generate();
            } while (!used.Add(code));
            profile.ReferralCode = code;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Backfilled referral codes for {Count} profiles", profiles.Count);
    }
}
