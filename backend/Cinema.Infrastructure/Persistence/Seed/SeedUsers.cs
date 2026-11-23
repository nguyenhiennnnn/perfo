using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed;

internal sealed record UserSeed(
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

internal static class SeedUsers
{
    public const int CustomerRoleId = 1;
    public const int EmployeeRoleId = 3;

    public const int PositionManagerId = 1;
    public const int PositionShiftLeadId = 2;
    public const int PositionTicketSellerId = 3;

    public static readonly PasswordHasher<User> Hasher = new();

    public static Task<User?> FindAsync(
        AppDbContext db,
        string email,
        CancellationToken cancellationToken
    ) => db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public static User Build(UserSeed seed, int roleId, ILogger logger)
    {
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
        user.PasswordHash = Hasher.HashPassword(user, password);

        logger.LogInformation(
            "Seeding test user: {Email} / {Password}",
            seed.Email,
            seed.GoogleId is null ? seed.Password : "(Google login)"
        );
        return user;
    }

    public static bool IsPasswordValid(User user, string password)
    {
        if (string.IsNullOrEmpty(user.PasswordHash))
            return false;

        try
        {
            return Hasher.VerifyHashedPassword(user, user.PasswordHash, password)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
