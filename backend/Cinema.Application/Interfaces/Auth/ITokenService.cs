using Cinema.Domain.Entities;

namespace Cinema.Application.Interfaces.Auth;

public record AccessTokenResult(string AccessToken, int ExpiresIn);

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user);

    AccessTokenResult GenerateSocketToken(User user);

    string GenerateOpaqueToken();

    string HashOpaqueToken(string token);
}
