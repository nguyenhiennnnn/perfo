namespace Cinema.Application.Interfaces.Auth;

public interface IUnsubscribeTokenService
{
    string CreateToken(int userId);

    string CreateUnsubscribeUrl(int userId);

    int? TryReadUserId(string token);
}
