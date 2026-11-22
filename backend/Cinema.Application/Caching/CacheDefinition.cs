namespace Cinema.Application.Caching;

public sealed record CacheDefinition(
    string Name,
    string Group,
    Func<IServiceProvider, CancellationToken, Task<object>> Loader,
    TimeSpan? Ttl
);
