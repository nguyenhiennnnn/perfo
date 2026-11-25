namespace Cinema.Application.Services.Loyalty;

public static class ReferralIdentityKeys
{
    private const string CountryCode = "84";

    public static IReadOnlyList<string> PhoneVariants(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return [];

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return [];

        var national = digits.StartsWith(CountryCode)
            ? digits[CountryCode.Length..]
            : digits.TrimStart('0');
        if (national.Length == 0)
            return [];

        return
        [
            phone.Trim(),
            $"0{national}",
            $"{CountryCode}{national}",
            $"+{CountryCode}{national}",
        ];
    }

    public static (string Plain, string Prefix, string Suffix) EmailKeys(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var at = normalized.LastIndexOf('@');
        var local = at < 0 ? normalized : normalized[..at];
        var domain = at < 0 ? string.Empty : normalized[at..];
        var plus = local.IndexOf('+');
        if (plus >= 0)
            local = local[..plus];

        return ($"{local}{domain}", $"{local}+", domain);
    }
}
