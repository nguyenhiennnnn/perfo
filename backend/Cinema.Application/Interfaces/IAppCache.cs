using Cinema.Application.Caching;

namespace Cinema.Application.Interfaces;

public interface IAppCache
{
    Task<T> GetAsync<T>(CacheKey<T> key, CancellationToken cancellationToken)
        where T : class;

    void Invalidate<T>(CacheKey<T> key)
        where T : class;

    void InvalidateGroup(string group);

    Task RefreshAsync<T>(CacheKey<T> key, CancellationToken cancellationToken)
        where T : class;

    Task RefreshGroupAsync(string group, CancellationToken cancellationToken);
}
