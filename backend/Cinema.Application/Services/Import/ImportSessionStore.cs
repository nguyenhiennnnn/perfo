using System.Collections.Concurrent;
using Cinema.Application.Interfaces.Import;
using Cinema.Application.Utils;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.Extensions.Caching.Memory;

namespace Cinema.Application.Services.Import;

public class ImportSessionStore(IMemoryCache cache) : IImportSessionStore
{
    private readonly ConcurrentDictionary<string, DateTime> expirations = new();

    public ImportSession<TRow> Create<TRow>(
        int userId,
        string? fileName,
        ImportFileFormat format,
        IReadOnlyList<string> headers,
        IReadOnlyList<ImportRow<TRow>> rows
    )
    {
        var session = new ImportSession<TRow>(
            Guid.NewGuid().ToString("N"),
            userId,
            fileName,
            format,
            headers,
            rows
        );

        var key = CacheKey(session.PreviewId);
        expirations[key] = DateTime.UtcNow.Add(ImportLimits.PreviewTtl);

        cache.Set(
            key,
            session,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ImportLimits.PreviewTtl,
            }.RegisterPostEvictionCallback(
                static (evictedKey, _, _, state) =>
                    ((ConcurrentDictionary<string, DateTime>)state!).TryRemove(
                        (string)evictedKey,
                        out _
                    ),
                expirations
            )
        );
        return session;
    }

    public ImportSession<TRow> Get<TRow>(string previewId, int userId)
    {
        if (
            cache.TryGetValue(CacheKey(previewId), out var cached)
            && cached is ImportSession<TRow> session
            && session.UserId == userId
        )
            return session;

        throw new NotFoundException(Messages.NotFound_ImportPreview);
    }

    public void Remove(string previewId) => cache.Remove(CacheKey(previewId));

    public int PurgeExpired()
    {
        var now = DateTime.UtcNow;
        var purged = 0;

        foreach (var (key, expiresAt) in expirations)
        {
            if (expiresAt > now)
                continue;

            cache.Remove(key);
            if (expirations.TryRemove(key, out _))
                purged++;
        }

        return purged;
    }

    private static string CacheKey(string previewId) => $"import:{previewId}";
}
