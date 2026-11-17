namespace Cinema.Application.Utils;

public static class SearchExtensions
{
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
