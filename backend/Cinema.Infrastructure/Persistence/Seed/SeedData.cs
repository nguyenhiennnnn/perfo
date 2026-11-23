using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Persistence.Seed;

public static class SeedData
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Role>()
            .HasData(
                new Role { RoleId = 1, RoleName = Roles.Customer },
                new Role { RoleId = 2, RoleName = Roles.Admin },
                new Role { RoleId = 3, RoleName = Roles.Employee }
            );

        modelBuilder
            .Entity<Position>()
            .HasData(
                new Position { PositionId = 1, Name = "Quản lý rạp" },
                new Position { PositionId = 2, Name = "Trưởng ca" },
                new Position { PositionId = 3, Name = "Nhân viên bán vé" }
            );

        modelBuilder
            .Entity<User>()
            .HasData(
                new User
                {
                    UserId = 1,
                    FullName = "System Admin",
                    Email = "admin@cinema.local",
                    PasswordHash =
                        "AQAAAAEAAYagAAAAENS3DOHbkUObJNrLWeaXshnI43hVryVZB/xVqXl+RRCO+rT1ek+EVigjK5BS5n9leQ==",
                    RoleId = 2,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = SeedDate,
                }
            );

        modelBuilder
            .Entity<Employee>()
            .HasData(
                new Employee
                {
                    EmployeeId = 1,
                    UserId = 1,
                    PositionId = 1,
                    HireDate = DateOnly.FromDateTime(SeedDate),
                    IsActive = true,
                }
            );
    }
}
