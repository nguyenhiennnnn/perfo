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

        modelBuilder
            .Entity<MembershipTier>()
            .HasData(
                new MembershipTier
                {
                    TierId = 1,
                    TierName = "Thành viên",
                    MinPoints = 0,
                    DiscountPercent = 0m,
                },
                new MembershipTier
                {
                    TierId = 2,
                    TierName = "Bạc",
                    MinPoints = 500,
                    DiscountPercent = 5m,
                },
                new MembershipTier
                {
                    TierId = 3,
                    TierName = "Vàng",
                    MinPoints = 2000,
                    DiscountPercent = 10m,
                },
                new MembershipTier
                {
                    TierId = 4,
                    TierName = "Kim Cương",
                    MinPoints = 5000,
                    DiscountPercent = 15m,
                }
            );

        modelBuilder
            .Entity<Combo>()
            .HasData(
                new Combo
                {
                    ComboId = 1,
                    Name = "Combo Solo",
                    Description = "1 Bắp ngọt (L) + 1 Nước ngọt (L)",
                    Price = 89000m,
                    ImageUrl = "https://images.unsplash.com/photo-1585647347483-22b66260dfff?w=500",
                    IsActive = true,
                },
                new Combo
                {
                    ComboId = 2,
                    Name = "Combo Couple",
                    Description = "1 Bắp ngọt (L) + 2 Nước ngọt (L)",
                    Price = 119000m,
                    ImageUrl = "https://images.unsplash.com/photo-1572177812156-58036aae439c?w=500",
                    IsActive = true,
                },
                new Combo
                {
                    ComboId = 3,
                    Name = "Combo Premium",
                    Description = "1 Bắp phô mai (L) + 2 Nước ngọt (L) + 1 Snack",
                    Price = 149000m,
                    ImageUrl = "https://images.unsplash.com/photo-1512149673953-1e251807ec7c?w=500",
                    IsActive = true,
                },
                new Combo
                {
                    ComboId = 4,
                    Name = "Combo Gia Đình",
                    Description = "2 Bắp ngọt (L) + 4 Nước ngọt (L) + 2 Snack",
                    Price = 249000m,
                    ImageUrl = "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?w=500",
                    IsActive = true,
                },
                new Combo
                {
                    ComboId = 5,
                    Name = "Combo Caramel",
                    Description = "1 Bắp caramel (L) + 2 Nước ngọt (L)",
                    Price = 135000m,
                    ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?w=500",
                    IsActive = true,
                }
            );
    }
}
