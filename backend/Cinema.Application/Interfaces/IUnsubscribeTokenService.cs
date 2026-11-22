namespace Cinema.Application.Interfaces;

public interface IUnsubscribeTokenService
{
    string CreateToken(int userId);

    string CreateUnsubscribeUrl(int userId);

    int? TryReadUserId(string token);
}
