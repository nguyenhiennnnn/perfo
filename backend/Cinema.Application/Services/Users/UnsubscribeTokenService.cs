using System.Security.Cryptography;
using System.Text;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Options;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Users;

public class UnsubscribeTokenService(
    IOptions<JwtOptions> jwtOptions,
    IOptions<AppUrlOptions> appUrlOptions
) : IUnsubscribeTokenService
{
    private const string Scope = "unsubscribe-marketing";

    public string CreateToken(int userId)
    {
        var payload = userId.ToString();
        return $"{Encode(Encoding.UTF8.GetBytes(payload))}.{Encode(Sign(payload))}";
    }

    public string CreateUnsubscribeUrl(int userId) =>
        $"{appUrlOptions.Value.FrontendBaseUrl.TrimEnd('/')}/unsubscribe?token={CreateToken(userId)}";

    public int? TryReadUserId(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var parts = token.Split('.');
        if (parts.Length != 2)
            return null;

        try
        {
            var payload = Encoding.UTF8.GetString(Decode(parts[0]));
            var signature = Decode(parts[1]);
            if (!CryptographicOperations.FixedTimeEquals(signature, Sign(payload)))
                return null;

            return int.TryParse(payload, out var userId) && userId > 0 ? userId : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private byte[] Sign(string payload)
    {
        var key = Encoding.UTF8.GetBytes(jwtOptions.Value.Secret);
        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{Scope}:{payload}"));
    }

    private static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static byte[] Decode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}
