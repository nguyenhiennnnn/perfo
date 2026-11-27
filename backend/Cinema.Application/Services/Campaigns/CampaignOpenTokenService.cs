using System.Security.Cryptography;
using System.Text;
using Cinema.Application.Interfaces.Campaigns;
using Cinema.Application.Options;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Campaigns;

public class CampaignOpenTokenService(
    IOptions<JwtOptions> jwtOptions,
    IOptions<AppUrlOptions> appUrlOptions
) : ICampaignOpenTokenService
{
    private const string Scope = "campaign-open";

    public string CreateOpenUrl(int campaignId, int userId)
    {
        var payload = $"{campaignId}:{userId}";
        var token = $"{Encode(Encoding.UTF8.GetBytes(payload))}.{Encode(Sign(payload))}";
        return $"{appUrlOptions.Value.ApiBaseUrl.TrimEnd('/')}/api/v1/campaign-tracking/{token}";
    }

    public (int CampaignId, int UserId)? TryRead(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var parts = token.Split('.');
        if (parts.Length != 2)
            return null;

        try
        {
            var payload = Encoding.UTF8.GetString(Decode(parts[0]));
            if (!CryptographicOperations.FixedTimeEquals(Decode(parts[1]), Sign(payload)))
                return null;

            var ids = payload.Split(':');
            return ids.Length == 2
                && int.TryParse(ids[0], out var campaignId)
                && int.TryParse(ids[1], out var userId)
                && campaignId > 0
                && userId > 0
                ? (campaignId, userId)
                : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private byte[] Sign(string payload) =>
        HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(jwtOptions.Value.Secret),
            Encoding.UTF8.GetBytes($"{Scope}:{payload}")
        );

    private static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static byte[] Decode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}
