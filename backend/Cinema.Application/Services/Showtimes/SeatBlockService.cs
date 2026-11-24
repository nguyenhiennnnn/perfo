using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Showtimes;

public class SeatBlockService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor,
    IRealtimeNotifier realtimeNotifier,
    ILogger<SeatBlockService> logger
) : ISeatBlockService
{
    public async Task<List<SeatBlockDto>> GetByShowtimeAsync(
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        if (!await db.Showtimes.AnyAsync(s => s.ShowtimeId == showtimeId, cancellationToken))
            throw new NotFoundException(Messages.NotFound_Showtime);

        return await ProjectDtos(db.SeatBlocks.AsNoTracking().Where(b => b.ShowtimeId == showtimeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SeatBlockDto>> BlockAsync(
        int showtimeId,
        BlockSeatsRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!SeatBlockRules.TryParseManualType(request.Type, out var type))
            throw new BusinessRuleException(Messages.Rule_SeatBlockTypeNotManual);

        return await BlockSeatsAsync(
            showtimeId,
            request.ShowtimeSeatIds,
            type,
            request.Reason,
            request.ExpiresAt,
            cancellationToken
        );
    }

    public async Task<UnblockSeatsResponse> UnblockAsync(
        int showtimeId,
        UnblockSeatsRequest request,
        CancellationToken cancellationToken
    )
    {
        var ids = request.ShowtimeSeatIds.Distinct().ToList();
        await UnblockSeatsAsync(showtimeId, ids, SeatBlockRules.ManualTypes, cancellationToken);
        return new UnblockSeatsResponse(showtimeId, ids);
    }

    public async Task<List<SeatBlockDto>> BlockSeatsAsync(
        int showtimeId,
        IReadOnlyCollection<int> showtimeSeatIds,
        SeatBlockType type,
        string reason,
        DateTime? expiresAt,
        CancellationToken cancellationToken
    )
    {
        var ids = DistinctSeatIds(showtimeSeatIds);

        var now = DateTime.UtcNow;
        if (expiresAt is { } expiry && expiry <= now)
            throw new ValidationAppException(
                Messages.Validation_SeatBlock_ExpiresAtPast,
                new Dictionary<string, string[]>
                {
                    ["expiresAt"] = [Messages.Validation_SeatBlock_ExpiresAtPast],
                }
            );

        await EnsureShowtimeActiveAsync(showtimeId, now, cancellationToken);

        var seats = await db
            .ShowtimeSeats.AsNoTracking()
            .Where(ss => ss.ShowtimeId == showtimeId && ids.Contains(ss.ShowtimeSeatId))
            .Select(ss => new { ss.ShowtimeSeatId, ss.Status })
            .ToListAsync(cancellationToken);
        if (seats.Count != ids.Count)
            throw new NotFoundException(Messages.NotFound_SeatInShowtime);

        var occupied = seats
            .Where(s => s.Status is ShowtimeSeatStatus.Held or ShowtimeSeatStatus.Booked)
            .Select(s => s.ShowtimeSeatId)
            .ToList();
        if (occupied.Count > 0)
            throw new SeatConflictException(Messages.Conflict_SeatBlockHasOrders, occupied);

        var alreadyBlocked = seats
            .Where(s => s.Status == ShowtimeSeatStatus.Blocked)
            .Select(s => s.ShowtimeSeatId)
            .ToList();
        if (alreadyBlocked.Count > 0)
            throw new SeatConflictException(
                Messages.Conflict_SeatBlockAlreadyBlocked,
                alreadyBlocked
            );

        var trimmedReason = reason.Trim();
        var actorUserId = actorAccessor.Current?.UserId;
        var blocks = ids.Select(id => new SeatBlock
            {
                ShowtimeId = showtimeId,
                ShowtimeSeatId = id,
                Type = type,
                Reason = trimmedReason,
                ExpiresAt = expiresAt,
                CreatedByUserId = actorUserId,
                CreatedAt = now,
            })
            .ToList();

        await db.InTransactionAsync(
            async () =>
            {
                var affected = await db
                    .ShowtimeSeats.Where(ss =>
                        ss.ShowtimeId == showtimeId
                        && ids.Contains(ss.ShowtimeSeatId)
                        && ss.Status == ShowtimeSeatStatus.Available
                    )
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.Status, ShowtimeSeatStatus.Blocked),
                        cancellationToken
                    );
                if (affected != ids.Count)
                    throw new SeatConflictException(Messages.Conflict_SeatBlockStateChanged, ids);

                db.SeatBlocks.AddRange(blocks);
                await db.SaveChangesAsync(cancellationToken);
                foreach (var block in blocks)
                    auditLogger.Record(
                        AuditActions.Create,
                        AuditEntityTypes.SeatBlock,
                        block.SeatBlockId.ToString(),
                        null,
                        AuditSnapshots.SeatBlockState(block)
                    );
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        await realtimeNotifier.NotifySeatBlockedAsync(showtimeId, ids, cancellationToken);
        logger.LogInformation(
            "SeatsBlocked ShowtimeId {ShowtimeId} Type {Type} SeatCount {Count}",
            showtimeId,
            type,
            ids.Count
        );

        var blockIds = blocks.Select(b => b.SeatBlockId).ToList();
        return await ProjectDtos(db.SeatBlocks.AsNoTracking().Where(b => blockIds.Contains(b.SeatBlockId)))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> UnblockSeatsAsync(
        int showtimeId,
        IReadOnlyCollection<int> showtimeSeatIds,
        IReadOnlyCollection<SeatBlockType>? allowedTypes,
        CancellationToken cancellationToken
    )
    {
        var ids = DistinctSeatIds(showtimeSeatIds);

        if (!await db.Showtimes.AnyAsync(s => s.ShowtimeId == showtimeId, cancellationToken))
            throw new NotFoundException(Messages.NotFound_Showtime);

        await db.InTransactionAsync(
            async () =>
            {
                var blocks = await db
                    .SeatBlocks.AsNoTracking()
                    .Where(b => b.ShowtimeId == showtimeId && ids.Contains(b.ShowtimeSeatId))
                    .ToListAsync(cancellationToken);
                if (blocks.Count != ids.Count)
                    throw new BusinessRuleException(Messages.Rule_SeatBlockSeatsNotBlocked);
                if (allowedTypes is not null && blocks.Any(b => !allowedTypes.Contains(b.Type)))
                    throw new BusinessRuleException(Messages.Rule_SeatBlockUnblockNotManual);

                await ReleaseBlocksAsync(showtimeId, blocks, strict: true, cancellationToken);
            },
            cancellationToken
        );

        await realtimeNotifier.NotifySeatReleasedAsync(showtimeId, ids, cancellationToken);
        logger.LogInformation(
            "SeatsUnblocked ShowtimeId {ShowtimeId} SeatCount {Count}",
            showtimeId,
            ids.Count
        );
        return ids.Count;
    }

    public async Task<int> ReleaseExpiredAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expired = await db
            .SeatBlocks.AsNoTracking()
            .Where(b => b.ExpiresAt != null && b.ExpiresAt <= now)
            .OrderBy(b => b.SeatBlockId)
            .Select(b => new { b.SeatBlockId, b.ShowtimeId })
            .Take(SeatBlockLimits.ReleaseBatchSize)
            .ToListAsync(cancellationToken);
        if (expired.Count == 0)
            return 0;

        var released = 0;
        foreach (var group in expired.GroupBy(b => b.ShowtimeId))
        {
            var showtimeId = group.Key;
            var blockIds = group.Select(b => b.SeatBlockId).ToList();
            var seatIds = new List<int>();
            try
            {
                await db.InTransactionAsync(
                    async () =>
                    {
                        var blocks = await db
                            .SeatBlocks.AsNoTracking()
                            .Where(b =>
                                blockIds.Contains(b.SeatBlockId)
                                && b.ExpiresAt != null
                                && b.ExpiresAt <= now
                            )
                            .ToListAsync(cancellationToken);
                        if (blocks.Count == 0)
                            return;

                        await ReleaseBlocksAsync(showtimeId, blocks, strict: false, cancellationToken);
                        seatIds.AddRange(blocks.Select(b => b.ShowtimeSeatId));
                    },
                    cancellationToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "ReleaseExpiredSeatBlocksFailed ShowtimeId {ShowtimeId}",
                    showtimeId
                );
                continue;
            }

            if (seatIds.Count == 0)
                continue;

            released += seatIds.Count;
            await realtimeNotifier.NotifySeatReleasedAsync(showtimeId, seatIds, cancellationToken);
        }

        if (released > 0)
            logger.LogInformation("SeatBlocksExpired Count {Count}", released);
        return released;
    }

    private async Task ReleaseBlocksAsync(
        int showtimeId,
        List<SeatBlock> blocks,
        bool strict,
        CancellationToken cancellationToken
    )
    {
        var seatIds = blocks.Select(b => b.ShowtimeSeatId).ToList();
        var released = await db
            .ShowtimeSeats.Where(ss =>
                ss.ShowtimeId == showtimeId
                && seatIds.Contains(ss.ShowtimeSeatId)
                && ss.Status == ShowtimeSeatStatus.Blocked
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.Status, ShowtimeSeatStatus.Available),
                cancellationToken
            );
        if (strict && released != blocks.Count)
            throw new ConflictAppException(Messages.Conflict_SeatBlockStateChanged);

        foreach (var block in blocks)
            auditLogger.Record(
                AuditActions.Delete,
                AuditEntityTypes.SeatBlock,
                block.SeatBlockId.ToString(),
                AuditSnapshots.SeatBlockState(block),
                null
            );
        var blockIds = blocks.Select(b => b.SeatBlockId).ToList();
        await db
            .SeatBlocks.Where(b => blockIds.Contains(b.SeatBlockId))
            .ExecuteDeleteAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureShowtimeActiveAsync(
        int showtimeId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var showtime =
            await db
                .Showtimes.AsNoTracking()
                .Where(s => s.ShowtimeId == showtimeId)
                .Select(s => new { s.Status, s.EndTime })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Showtime);
        if (showtime.Status != ShowtimeStatus.Scheduled || showtime.EndTime <= now)
            throw new BusinessRuleException(Messages.Rule_SeatBlockShowtimeNotActive);
    }

    private static List<int> DistinctSeatIds(IReadOnlyCollection<int> showtimeSeatIds)
    {
        var ids = showtimeSeatIds.Distinct().ToList();
        if (ids.Count > 0 && ids.Count <= SeatBlockLimits.MaxSeatsPerRequest)
            return ids;

        var message = ids.Count == 0
            ? Messages.Validation_Seats_AtLeastOne
            : string.Format(
                Messages.Validation_SeatBlock_TooManySeats,
                SeatBlockLimits.MaxSeatsPerRequest
            );
        throw new ValidationAppException(
            message,
            new Dictionary<string, string[]> { ["showtimeSeatIds"] = [message] }
        );
    }

    private static IQueryable<SeatBlockDto> ProjectDtos(IQueryable<SeatBlock> query) =>
        query
            .OrderBy(b => b.ShowtimeSeat.Seat.SeatRow.Length)
            .ThenBy(b => b.ShowtimeSeat.Seat.SeatRow)
            .ThenBy(b => b.ShowtimeSeat.Seat.SeatNumber)
            .Select(b => new SeatBlockDto(
                b.SeatBlockId,
                b.ShowtimeId,
                b.ShowtimeSeatId,
                b.ShowtimeSeat.Seat.SeatRow,
                b.ShowtimeSeat.Seat.SeatNumber,
                b.Type.ToString(),
                b.Reason,
                b.ExpiresAt,
                b.CreatedByUserId,
                b.CreatedAt
            ));
}
