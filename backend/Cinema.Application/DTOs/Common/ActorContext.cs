using Cinema.Shared.Constants;

namespace Cinema.Application.DTOs.Common;

public sealed record ActorContext(int UserId, string Role, string? IpAddress, string? UserAgent)
{
    public bool IsAdmin => string.Equals(Role, Roles.Admin, StringComparison.Ordinal);
}
