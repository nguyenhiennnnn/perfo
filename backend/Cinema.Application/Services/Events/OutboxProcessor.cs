using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Events;

public class OutboxProcessor(
    IAppDbContext db,
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobOptions> options,
    ILogger<OutboxProcessor> logger
) : IOutboxProcessor
{
    private readonly BackgroundJobOptions _options = options.Value;

    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dueIds = await db
            .OutboxDeliveries.AsNoTracking()
            .Where(d =>
                (d.Status == OutboxDeliveryStatus.Pending && d.NextAttemptAt <= now)
                || (d.Status == OutboxDeliveryStatus.Processing && d.LockedUntil < now)
            )
            .OrderBy(d => d.NextAttemptAt)
            .ThenBy(d => d.OutboxDeliveryId)
            .Select(d => d.OutboxDeliveryId)
            .Take(Math.Max(1, _options.OutboxBatchSize))
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var deliveryId in dueIds)
            if (await ProcessOneAsync(deliveryId, cancellationToken))
                processed++;

        return processed;
    }

    public Task<int> PurgeProcessedAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, _options.OutboxRetentionDays));
        return db
            .OutboxMessages.Where(m =>
                m.CreatedAt < cutoff
                && m.Deliveries.All(d => d.Status == OutboxDeliveryStatus.Processed)
            )
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<bool> ProcessOneAsync(long deliveryId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var leaseUntil = now.AddSeconds(Math.Max(1, _options.OutboxLeaseSeconds));

        var claimed = await db
            .OutboxDeliveries.Where(d =>
                d.OutboxDeliveryId == deliveryId
                && (
                    (d.Status == OutboxDeliveryStatus.Pending && d.NextAttemptAt <= now)
                    || (d.Status == OutboxDeliveryStatus.Processing && d.LockedUntil < now)
                )
            )
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(d => d.Status, OutboxDeliveryStatus.Processing)
                        .SetProperty(d => d.LockedUntil, leaseUntil)
                        .SetProperty(d => d.Attempts, d => d.Attempts + 1),
                cancellationToken
            );
        if (claimed == 0)
            return false;

        var delivery = await db
            .OutboxDeliveries.AsNoTracking()
            .Include(d => d.Message)
            .FirstAsync(d => d.OutboxDeliveryId == deliveryId, cancellationToken);

        try
        {
            var descriptor =
                BusinessEventCatalog.Find(delivery.Message.EventType)
                ?? throw new OutboxPermanentFailureException(
                    string.Format(Messages.Event_TypeNotRegistered, delivery.Message.EventType)
                );

            using var scope = scopeFactory.CreateScope();
            var handled = await descriptor.RunBackgroundHandlerAsync(
                scope.ServiceProvider,
                delivery.HandlerName,
                delivery.Message.Payload,
                cancellationToken
            );
            if (!handled)
                throw new OutboxPermanentFailureException(
                    string.Format(
                        Messages.Event_HandlerNotRegistered,
                        delivery.HandlerName,
                        delivery.Message.EventType
                    )
                );

            await db
                .OutboxDeliveries.Where(d =>
                    d.OutboxDeliveryId == deliveryId && d.Status == OutboxDeliveryStatus.Processing
                )
                .ExecuteUpdateAsync(
                    s =>
                        s.SetProperty(d => d.Status, OutboxDeliveryStatus.Processed)
                            .SetProperty(d => d.ProcessedAt, DateTime.UtcNow)
                            .SetProperty(d => d.LockedUntil, (DateTime?)null)
                            .SetProperty(d => d.LastError, (string?)null),
                    CancellationToken.None
                );
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await RecordFailureAsync(delivery.OutboxDeliveryId, delivery.Attempts, ex);
            return false;
        }
    }

    private async Task RecordFailureAsync(long deliveryId, int attempts, Exception exception)
    {
        var permanent = exception is OutboxPermanentFailureException;
        var exhausted = attempts >= Math.Max(1, _options.OutboxMaxAttempts);
        var error = Truncate(exception.Message);

        if (permanent || exhausted)
        {
            await db
                .OutboxDeliveries.Where(d =>
                    d.OutboxDeliveryId == deliveryId && d.Status == OutboxDeliveryStatus.Processing
                )
                .ExecuteUpdateAsync(
                    s =>
                        s.SetProperty(d => d.Status, OutboxDeliveryStatus.Failed)
                            .SetProperty(d => d.LockedUntil, (DateTime?)null)
                            .SetProperty(d => d.LastError, error),
                    CancellationToken.None
                );
            logger.LogError(
                exception,
                "OutboxDeliveryFailed DeliveryId {DeliveryId} Attempts {Attempts}",
                deliveryId,
                attempts
            );
            return;
        }

        var nextAttemptAt = DateTime.UtcNow.AddSeconds(RetryDelaySeconds(attempts));
        await db
            .OutboxDeliveries.Where(d =>
                d.OutboxDeliveryId == deliveryId && d.Status == OutboxDeliveryStatus.Processing
            )
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(d => d.Status, OutboxDeliveryStatus.Pending)
                        .SetProperty(d => d.NextAttemptAt, nextAttemptAt)
                        .SetProperty(d => d.LockedUntil, (DateTime?)null)
                        .SetProperty(d => d.LastError, error),
                CancellationToken.None
            );
        logger.LogWarning(
            exception,
            "OutboxDeliveryRetryScheduled DeliveryId {DeliveryId} Attempts {Attempts} NextAttemptAt {NextAttemptAt}",
            deliveryId,
            attempts,
            nextAttemptAt
        );
    }

    private int RetryDelaySeconds(int attempts)
    {
        var delays = _options.OutboxRetryDelaysSeconds;
        if (delays.Length == 0)
            return 60;

        return delays[Math.Clamp(attempts - 1, 0, delays.Length - 1)];
    }

    private static string Truncate(string value) =>
        value.Length > FieldLengths.ErrorMessage ? value[..FieldLengths.ErrorMessage] : value;
}

public sealed class OutboxPermanentFailureException(string message) : Exception(message);
