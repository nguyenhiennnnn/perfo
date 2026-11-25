using System.Collections.Concurrent;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Shared.Resources;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Cinema.Application.Caching;

public sealed class AppCache : IAppCache
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

    private readonly IMemoryCache _memory;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<AppCache> _logger;
    private readonly Dictionary<string, CacheDefinition> _definitions;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();
    private readonly ConcurrentDictionary<string, long> _versions = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _groupSources = new();
    private readonly object _groupLock = new();

    public AppCache(
        IMemoryCache memory,
        IServiceScopeFactory scopes,
        IEnumerable<CacheDefinition> definitions,
        ILogger<AppCache> logger
    )
    {
        _memory = memory;
        _scopes = scopes;
        _logger = logger;
        _definitions = new Dictionary<string, CacheDefinition>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            if (!_definitions.TryAdd(definition.Name, definition))
                throw new InvalidOperationException(
                    string.Format(Messages.Config_CacheKeyDuplicated, definition.Name)
                );
        }
    }

    public async Task<T> GetAsync<T>(CacheKey<T> key, CancellationToken cancellationToken)
        where T : class
    {
        var definition = Resolve(key.Name);
        if (_memory.TryGetValue(key.Name, out object? hit) && hit is T cached)
            return cached;

        var gate = _gates.GetOrAdd(key.Name, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_memory.TryGetValue(key.Name, out hit) && hit is T cachedAfterWait)
                return cachedAfterWait;

            return (T)await LoadAndStoreAsync(definition, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Invalidate<T>(CacheKey<T> key)
        where T : class => InvalidateDefinition(Resolve(key.Name));

    public void InvalidateGroup(string group)
    {
        lock (_groupLock)
        {
            if (_groupSources.TryRemove(group, out var source))
            {
                source.Cancel();
            }
        }
    }

    public async Task RefreshAsync<T>(CacheKey<T> key, CancellationToken cancellationToken)
        where T : class
    {
        var definition = Resolve(key.Name);
        InvalidateDefinition(definition);
        await TryReloadAsync(definition, cancellationToken);
    }

    public async Task RefreshGroupAsync(string group, CancellationToken cancellationToken)
    {
        InvalidateGroup(group);
        foreach (var definition in _definitions.Values.Where(d => d.Group == group))
            await TryReloadAsync(definition, cancellationToken);
    }

    private CacheDefinition Resolve(string name) =>
        _definitions.TryGetValue(name, out var definition)
            ? definition
            : throw new InvalidOperationException(
                string.Format(Messages.Config_CacheKeyNotRegistered, name)
            );

    private void InvalidateDefinition(CacheDefinition definition)
    {
        _versions.AddOrUpdate(definition.Name, 1, (_, version) => version + 1);
        _memory.Remove(definition.Name);
    }

    private async Task TryReloadAsync(
        CacheDefinition definition,
        CancellationToken cancellationToken
    )
    {
        var gate = _gates.GetOrAdd(definition.Name, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await LoadAndStoreAsync(definition, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache refresh failed for {CacheKey}", definition.Name);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<object> LoadAndStoreAsync(
        CacheDefinition definition,
        CancellationToken cancellationToken
    )
    {
        var version = _versions.GetValueOrDefault(definition.Name);
        var groupToken = GroupToken(definition.Group);

        await using var scope = _scopes.CreateAsyncScope();
        var value = await definition.Loader(scope.ServiceProvider, cancellationToken);

        if (
            _versions.GetValueOrDefault(definition.Name) == version
            && !groupToken.IsCancellationRequested
        )
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(definition.Ttl ?? DefaultTtl)
                .AddExpirationToken(new CancellationChangeToken(groupToken));
            _memory.Set(definition.Name, value, options);
        }

        return value;
    }

    private CancellationToken GroupToken(string group)
    {
        lock (_groupLock)
        {
            return _groupSources.GetOrAdd(group, _ => new CancellationTokenSource()).Token;
        }
    }
}
