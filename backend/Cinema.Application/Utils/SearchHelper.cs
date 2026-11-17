using System.Text.RegularExpressions;

namespace Cinema.Application.Utils;
public static partial class SearchHelper
{
    [GeneratedRegex(@"[\s\-\(\)\.]+", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneCleanupRegex();

    [GeneratedRegex(@"^\+84", RegexOptions.CultureInvariant)]
    private static partial Regex Plus84Regex();

    public static List<string> Tokenize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        return [.. input.Split((char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    public static string EscapeLike(string token) =>
        token
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%",  "\\%",  StringComparison.Ordinal)
            .Replace("_",  "\\_",  StringComparison.Ordinal);

    public static string ILikePattern(string token) => $"%{EscapeLike(token)}%";

    public static string NormalizePhone(string token)
    {
        var cleaned = PhoneCleanupRegex().Replace(token, "");
        return Plus84Regex().Replace(cleaned, "0");
    }
}
