using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class AccountSeedGroup : ISeedGroup
{
    public string Name => "accounts";

    public Task SeedAsync(SeedContext context, CancellationToken cancellationToken) =>
        SeedTestAccountsAsync(context.Db, context.Logger, cancellationToken);

    private const int CustomerRoleId = 1;
    private const int EmployeeRoleId = 3;

    private const int PositionManagerId = 1;
    private const int PositionShiftLeadId = 2;
    private const int PositionTicketSellerId = 3;

    private sealed record UserSeed(
        string Email,
        string Password,
        string FullName,
        string Phone,
        Gender Gender,
        DateOnly DateOfBirth,
        bool IsActive = true,
        bool EmailConfirmed = true,
        string? GoogleId = null
    );

    private sealed record CustomerSeed(UserSeed User, int Points, decimal TotalSpent);

    private sealed record EmployeeSeed(
        UserSeed User,
        int PositionId,
        bool IsActive = true,
        DateOnly? HireDate = null
    );

    private static readonly CustomerSeed[] TestCustomers =
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

    private static readonly EmployeeSeed[] TestEmployees =
    [
        new(
            new UserSeed(
                SeedEmails.Employee,
                "Employee@123",
                "Đặng Hoàng Nam",
                "0944176328",
                Gender.Male,
                new DateOnly(1998, 6, 6)
            ),
            PositionTicketSellerId,
            HireDate: new DateOnly(2026, 2, 1)
        ),
        new(
            new UserSeed(
                SeedEmails.Manager,
                "Employee@123",
                "Bùi Thị Thanh Hương",
                "0986453217",
                Gender.Female,
                new DateOnly(1985, 8, 12)
            ),
            PositionManagerId,
            HireDate: new DateOnly(2026, 1, 15)
        ),
        new(
            new UserSeed(
                SeedEmails.ShiftLead,
                "Employee@123",
                "Ngô Văn Tuấn",
                "0919807264",
                Gender.Male,
                new DateOnly(1992, 2, 28)
            ),
            PositionShiftLeadId,
            HireDate: new DateOnly(2026, 1, 20)
        ),
        new(
            new UserSeed(
                SeedEmails.EmployeeInactive,
                "Employee@123",
                "Lý Quốc Hải",
                "0583296410",
                Gender.Male,
                new DateOnly(1994, 10, 10)
            ),
            PositionTicketSellerId,
            IsActive: false,
            HireDate: new DateOnly(2026, 1, 25)
        ),
    ];

    private static async Task SeedTestAccountsAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var hasher = new PasswordHasher<User>();

        foreach (var seed in TestCustomers)
        {
            var user = await EnsureUserAsync(
                db,
                hasher,
                logger,
                seed.User,
                CustomerRoleId,
                cancellationToken
            );

            var hasProfile = await db.CustomerProfiles.AnyAsync(
                p => p.UserId == user.UserId,
                cancellationToken
            );
            if (hasProfile)
                continue;

            var tier = await db
                .MembershipTiers.Where(t => t.MinPoints <= seed.Points)
                .OrderByDescending(t => t.MinPoints)
                .FirstOrDefaultAsync(cancellationToken);

            db.CustomerProfiles.Add(
                new CustomerProfile
                {
                    UserId = user.UserId,
                    TierId = tier?.TierId,
                    Points = seed.Points,
                    TotalSpent = seed.TotalSpent,
                }
            );
            if (seed.Points > 0)
                db.LoyaltyPointEntries.Add(
                    new LoyaltyPointEntry
                    {
                        UserId = user.UserId,
                        Points = seed.Points,
                        EarnedAt = DateTime.UtcNow,
                    }
                );
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var seed in TestEmployees)
        {
            var user = await EnsureUserAsync(
                db,
                hasher,
                logger,
                seed.User,
                EmployeeRoleId,
                cancellationToken
            );

            var hasEmployee = await db.Employees.AnyAsync(
                e => e.UserId == user.UserId,
                cancellationToken
            );
            if (hasEmployee)
                continue;

            db.Employees.Add(
                new Employee
                {
                    UserId = user.UserId,
                    PositionId = seed.PositionId,
                    HireDate = seed.HireDate,
                    IsActive = seed.IsActive,
                }
            );
            await db.SaveChangesAsync(cancellationToken);
        }

        var adminUser = await db.Users.FirstOrDefaultAsync(
            u => u.Email == SeedEmails.Admin,
            cancellationToken
        );
        if (adminUser != null && !IsPasswordValid(hasher, adminUser, "Admin@123"))
        {
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123");
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Reset admin password hash to match documented Admin@123.");
        }
    }

    private static async Task<User> EnsureUserAsync(
        AppDbContext db,
        PasswordHasher<User> hasher,
        ILogger logger,
        UserSeed seed,
        int roleId,
        CancellationToken cancellationToken
    )
    {
        var existing = await db.Users.FirstOrDefaultAsync(
            u => u.Email == seed.Email,
            cancellationToken
        );
        if (existing != null)
            return existing;

        var user = new User
        {
            FullName = seed.FullName,
            Email = seed.Email,
            Phone = seed.Phone,
            Gender = seed.Gender,
            DateOfBirth = seed.DateOfBirth,
            GoogleId = seed.GoogleId,
            RoleId = roleId,
            IsActive = seed.IsActive,
            EmailConfirmed = seed.EmailConfirmed,
            CreatedAt = DateTime.UtcNow,
        };
        var password = seed.GoogleId is null
            ? seed.Password
            : Guid.NewGuid().ToString("N") + "Aa1!";
        user.PasswordHash = hasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded test user: {Email} / {Password}",
            seed.Email,
            seed.GoogleId is null ? seed.Password : "(Google login)"
        );
        return user;
    }

    private static bool IsPasswordValid(PasswordHasher<User> hasher, User user, string password)
    {
        if (string.IsNullOrEmpty(user.PasswordHash))
            return false;

        try
        {
            return hasher.VerifyHashedPassword(user, user.PasswordHash, password)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
