using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class StaffSeedGroup : ISeedGroup
{
    private const string AdminPassword = "Admin@123";

    private sealed record EmployeeSeed(
        UserSeed User,
        int PositionId,
        bool IsActive = true,
        DateOnly? HireDate = null
    );

    private static readonly EmployeeSeed[] Employees =
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
            SeedUsers.PositionTicketSellerId,
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
            SeedUsers.PositionManagerId,
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
            SeedUsers.PositionShiftLeadId,
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
            SeedUsers.PositionTicketSellerId,
            IsActive: false,
            HireDate: new DateOnly(2026, 1, 25)
        ),
    ];

    public string Name => "staff";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;

        foreach (var seed in Employees)
        {
            var user = await SeedUsers.FindAsync(db, seed.User.Email, cancellationToken);
            if (user is null)
            {
                user = SeedUsers.Build(seed.User, SeedUsers.EmployeeRoleId, context.Logger);
                db.Users.Add(user);
            }
            else if (await db.Employees.AnyAsync(e => e.UserId == user.UserId, cancellationToken))
            {
                continue;
            }

            db.Employees.Add(
                new Employee
                {
                    User = user,
                    PositionId = seed.PositionId,
                    HireDate = seed.HireDate,
                    IsActive = seed.IsActive,
                }
            );
            await db.SaveChangesAsync(cancellationToken);
        }

        await SyncAdminPasswordAsync(context, cancellationToken);
    }

    private static async Task SyncAdminPasswordAsync(
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var admin = await SeedUsers.FindAsync(context.Db, SeedEmails.Admin, cancellationToken);
        if (admin is null || SeedUsers.IsPasswordValid(admin, AdminPassword))
            return;

        admin.PasswordHash = SeedUsers.Hasher.HashPassword(admin, AdminPassword);
        await context.Db.SaveChangesAsync(cancellationToken);
        context.Logger.LogInformation(
            "Reset admin password hash to match documented {Password}.",
            AdminPassword
        );
    }
}
