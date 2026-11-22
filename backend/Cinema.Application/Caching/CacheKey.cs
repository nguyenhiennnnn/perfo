namespace Cinema.Application.Caching;

public sealed record CacheKey<T>(string Name, string Group)
    where T : class;
