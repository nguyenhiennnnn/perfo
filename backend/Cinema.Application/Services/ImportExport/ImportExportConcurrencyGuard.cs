using System.Collections.Concurrent;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Options;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.ImportExport;

public sealed class ImportExportConcurrencyGuard(
    IServiceScopeFactory scopeFactory,
    IOptions<ImportExportOptions> options,
    ILogger<ImportExportConcurrencyGuard> logger
) : IImportExportConcurrencyGuard
{
    private readonly ConcurrentDictionary<int, UserSlot> slots = new();

    public async Task<IDisposable> AcquireAsync(int userId, CancellationToken cancellationToken)
    {
        var slot = slots.GetOrAdd(userId, _ => new UserSlot());
        await slot.Gate.WaitAsync(cancellationToken);

        try
        {
            var max = Math.Max(1, options.Value.MaxActiveJobsPerUser);
            var active = await CountActiveJobsAsync(userId, cancellationToken) + slot.InFlight;

            if (active >= max)
            {
                logger.LogWarning(
                    "ImportExportConcurrencyRejected UserId {UserId} Active {Active} Max {Max}",
                    userId,
                    active,
                    max
                );
                throw new BusinessRuleException(
                    string.Format(Messages.Rule_ImportExportTooManyActiveJobs, active, max)
                );
            }

            Interlocked.Increment(ref slot.InFlight);
            return new Lease(slot);
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    private async Task<int> CountActiveJobsAsync(int userId, CancellationToken cancellationToken)
    {
        var staleBefore = DateTime.UtcNow.AddMinutes(-Math.Max(1, options.Value.ActiveJobStaleMinutes));

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        return await db
            .ImportExportJobs.AsNoTracking()
            .CountAsync(
                j =>
                    j.CreatedByUserId == userId
                    && (
                        j.Status == ImportExportJobStatus.Queued
                        || j.Status == ImportExportJobStatus.Running
                    )
                    && j.CreatedAt > staleBefore,
                cancellationToken
            );
    }

    private sealed class UserSlot
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public int InFlight;
    }

    private sealed class Lease(UserSlot slot) : IDisposable
    {
        private int released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref released, 1) == 0)
                Interlocked.Decrement(ref slot.InFlight);
        }
    }
}
