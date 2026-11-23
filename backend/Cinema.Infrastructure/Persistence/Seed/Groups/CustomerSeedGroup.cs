using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class CustomerSeedGroup : ISeedGroup
{
    private sealed record CustomerSeed(UserSeed User, int Points, decimal TotalSpent);

    private sealed record OptIns(bool Marketing, bool Personalized, bool Reminder);

    private static readonly Dictionary<string, OptIns> OptInsByEmail = new()
    {
        [SeedEmails.Customer1] = new(true, true, true),
        [SeedEmails.Customer2] = new(false, true, false),
        [SeedEmails.Customer3] = new(true, false, true),
        [SeedEmails.Customer4] = new(true, true, true),
        [SeedEmails.CustomerNew] = new(false, false, false),
    };

    private static readonly CustomerSeed[] Customers =
    [
        new(
            new UserSeed(
                SeedEmails.Customer1,
                "Customer@123",
                "Nguyễn Minh Khoa",
                "0903482917",
                Gender.Male,
                new DateOnly(1995, 3, 14)
            ),
            300,
            3_000_000m
        ),
        new(
            new UserSeed(
                SeedEmails.Customer2,
                "Customer@123",
                "Trần Thị Thu Hà",
                "0938215604",
                Gender.Female,
                new DateOnly(1993, 7, 2)
            ),
            800,
            8_000_000m
        ),
        new(
            new UserSeed(
                SeedEmails.Customer3,
                "Customer@123",
                "Lê Quốc Bảo",
                "0977360281",
                Gender.Male,
                new DateOnly(1988, 11, 25)
            ),
            2_500,
            25_000_000m
        ),
        new(
            new UserSeed(
                SeedEmails.Customer4,
                "Customer@123",
                "Phạm Ngọc Diệp",
                "0912745893",
                Gender.Female,
                new DateOnly(1990, 1, 9)
            ),
            6_000,
            60_000_000m
        ),
        new(
            new UserSeed(
                SeedEmails.CustomerNew,
                "Customer@123",
                "Hoàng Minh Anh",
                "0868129355",
                Gender.Other,
                new DateOnly(2001, 5, 20)
            ),
            0,
            0m
        ),
        new(
            new UserSeed(
                SeedEmails.CustomerInactive,
                "Customer@123",
                "Vũ Thị Lan Anh",
                "0356908142",
                Gender.Female,
                new DateOnly(1997, 9, 30),
                IsActive: false
            ),
            100,
            1_000_000m
        ),
        new(
            new UserSeed(
                SeedEmails.CustomerUnverified,
                "Customer@123",
                "Đỗ Thành Đạt",
                "0702531786",
                Gender.Male,
                new DateOnly(1999, 12, 1),
                EmailConfirmed: false
            ),
            0,
            0m
        ),
        new(
            new UserSeed(
                SeedEmails.CustomerGoogle,
                "Google@123",
                "Ngô Thùy Dương",
                "0829614073",
                Gender.Female,
                new DateOnly(1996, 4, 18),
                GoogleId: "seed-google-sub-000000000001"
            ),
            0,
            0m
        ),
    ];

    public string Name => "customers";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;

        var tiers = await db
            .MembershipTiers.OrderByDescending(t => t.MinPoints)
            .ToListAsync(cancellationToken);

        foreach (var seed in Customers)
        {
            var user = await SeedUsers.FindAsync(db, seed.User.Email, cancellationToken);
            if (user is null)
            {
                user = SeedUsers.Build(seed.User, SeedUsers.CustomerRoleId, context.Logger);
                db.Users.Add(user);
            }
            else if (
                await db.CustomerProfiles.AnyAsync(p => p.UserId == user.UserId, cancellationToken)
            )
            {
                continue;
            }

            var tier = tiers.FirstOrDefault(t => t.MinPoints <= seed.Points);
            var optIns = OptInsByEmail.GetValueOrDefault(seed.User.Email);
            db.CustomerProfiles.Add(
                new CustomerProfile
                {
                    User = user,
                    TierId = tier?.TierId,
                    Points = seed.Points,
                    TotalSpent = seed.TotalSpent,
                    MarketingOptIn = optIns?.Marketing ?? false,
                    PersonalizedRecommendations = optIns?.Personalized ?? true,
                    ReminderAndReviewInvites = optIns?.Reminder ?? true,
                }
            );

            if (seed.Points > 0)
            {
                var hasInitialEntry =
                    user.UserId != 0
                    && await db.LoyaltyPointEntries.AnyAsync(
                        e => e.UserId == user.UserId && e.OrderId == null,
                        cancellationToken
                    );
                if (!hasInitialEntry)
                    db.LoyaltyPointEntries.Add(
                        new LoyaltyPointEntry
                        {
                            User = user,
                            Points = seed.Points,
                            EarnedAt = DateTime.UtcNow,
                        }
                    );
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
