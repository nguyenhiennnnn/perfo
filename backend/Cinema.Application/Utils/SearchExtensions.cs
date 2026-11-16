namespace Cinema.Application.Utils;

/// <summary>
/// Extension methods that apply multi-keyword, accent-insensitive, case-insensitive search
/// to <see cref="IQueryable{T}"/> using PostgreSQL <c>unaccent()</c> + <c>ILIKE</c>.
/// </summary>
public static class SearchExtensions
{
    /// <summary>
    /// Splits <paramref name="search"/> into tokens and for each token invokes
    /// <paramref name="applyToken"/>(query, pattern) where <c>pattern</c> is the
    /// ILike containment pattern <c>%token%</c>.
    /// All tokens must match (AND semantics); returns <paramref name="query"/> unchanged
    /// when <paramref name="search"/> is null/whitespace.
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(
        this IQueryable<T> query,
        string? search,
        Func<IQueryable<T>, string, IQueryable<T>> applyToken)
    {
        var tokens = SearchHelper.Tokenize(search);
        foreach (var token in tokens)
            query = applyToken(query, SearchHelper.ILikePattern(token));
        return query;
    }

    /// <summary>
    /// Variant that passes both a plain-text pattern and a phone-normalised pattern to
    /// <paramref name="applyToken"/> so callers can OR them on phone columns.
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(
        this IQueryable<T> query,
        string? search,
        Func<IQueryable<T>, string, string, IQueryable<T>> applyToken)
    {
        var tokens = SearchHelper.Tokenize(search);
        foreach (var token in tokens)
        {
            var textPattern  = SearchHelper.ILikePattern(token);
            var phonePattern = SearchHelper.ILikePattern(SearchHelper.NormalizePhone(token));
            query = applyToken(query, textPattern, phonePattern);
        }
        return query;
    }
}
