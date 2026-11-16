using System.Text.RegularExpressions;

namespace Cinema.Application.Utils;

/// <summary>
/// Helpers for building accent-insensitive, case-insensitive, multi-keyword EF Core search queries
/// against PostgreSQL using <c>unaccent()</c> + <c>ILIKE</c>.
/// </summary>
public static partial class SearchHelper
{
    // Strips spaces, dashes, dots and parentheses from phone strings.
    [GeneratedRegex(@"[\s\-\(\)\.]+", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneCleanupRegex();

    // Matches the +84 country-code prefix.
    [GeneratedRegex(@"^\+84", RegexOptions.CultureInvariant)]
    private static partial Regex Plus84Regex();

    /// <summary>
    /// Splits <paramref name="input"/> into whitespace-separated, non-empty tokens.
    /// Returns an empty list when <paramref name="input"/> is <see langword="null"/> or whitespace.
    /// </summary>
    public static List<string> Tokenize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        return [.. input.Split((char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    /// <summary>
    /// Escapes PostgreSQL LIKE / ILike special characters (<c>\</c>, <c>%</c>, <c>_</c>).
    /// </summary>
    public static string EscapeLike(string token) =>
        token
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%",  "\\%",  StringComparison.Ordinal)
            .Replace("_",  "\\_",  StringComparison.Ordinal);

    /// <summary>
    /// Wraps <paramref name="token"/> in an ILike containment pattern: <c>%{escaped}%</c>.
    /// </summary>
    public static string ILikePattern(string token) => $"%{EscapeLike(token)}%";

    /// <summary>
    /// Normalises a phone-number token for database comparison:
    /// strips spaces / dashes / dots / parentheses and converts a leading <c>+84</c> to <c>0</c>.
    /// </summary>
    public static string NormalizePhone(string token)
    {
        var cleaned = PhoneCleanupRegex().Replace(token, "");
        return Plus84Regex().Replace(cleaned, "0");
    }
}
