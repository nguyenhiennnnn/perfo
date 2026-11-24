using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.Rooms;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Showtimes;

public class ShowtimeRoomChangeService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor,
    IBusinessEventDispatcher eventDispatcher,
    IRealtimeNotifier realtimeNotifier,
    ILogger<ShowtimeRoomChangeService> logger
) : IShowtimeRoomChangeService
{
    public async Task<RoomChangePreviewResponse> PreviewAsync(
        int showtimeId,
        ShowtimeRoomChangeRequest request,
        CancellationToken cancellationToken
    )
    {
        var plan = await BuildPlanAsync(showtimeId, request, null, cancellationToken);
        var sourcesById = plan.Sources.ToDictionary(s => s.ShowtimeSeatId);
        var targetsById = plan.Targets.ToDictionary(t => t.SeatId);
        var dueBySeat = plan
            .Dues.GroupBy(d => d.ShowtimeSeatId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Amount));

        var mappings = plan
            .Mappings.Select(m =>
            {
                var source = sourcesById[m.SourceId];
                var target = m.TargetId is { } targetId ? targetsById[targetId] : null;
                return new RoomChangeSeatMappingDto(
                    source.ShowtimeSeatId,
                    source.Status.ToString(),
                    source.HasOrders,
                    ToRef(source.Row, source.Number, source.SeatTypeId, source.SeatTypeName, source.IsCouple),
                    target?.SeatId,
                    target is null
                        ? null
                        : ToRef(
                            target.Row,
                            target.Number,
                            target.SeatTypeId,
                            target.SeatTypeName,
                            target.IsCouple
                        ),
                    m.Kind.ToString(),
                    NeedsReview(m, source),
                    dueBySeat.GetValueOrDefault(source.ShowtimeSeatId)
                );
            })
            .OrderBy(m => ShowtimeSeatRemapper.RowIndex(m.Source.Row))
            .ThenBy(m => m.Source.Number)
            .ToList();

        var targetSeats = plan
            .Targets.OrderBy(t => ShowtimeSeatRemapper.RowIndex(t.Row))
            .ThenBy(t => t.Number)
            .Select(t => new RoomChangeTargetSeatDto(
                t.SeatId,
                t.Row,
                t.Number,
                t.SeatTypeId,
                t.SeatTypeName,
                t.IsCouple,
                t.CoupleId
            ))
            .ToList();

        return new RoomChangePreviewResponse(
            showtimeId,
            plan.Source.RoomId,
            plan.Source.RoomName,
            plan.Target.RoomId,
            plan.Target.RoomName,
            plan.Issues.Count == 0,
            [.. plan.Issues.Select(i => i.Message)],
            plan.Sources.Count(s => s.Required),
            plan.Targets.Count,
            mappings.Count(m => m.NeedsReview),
            plan.Dues.Count,
            plan.Dues.Sum(d => d.Amount),
            mappings,
            targetSeats
        );
    }

    public async Task<ShowtimeRoomChangeResponse> ApplyAsync(
        int showtimeId,
        ShowtimeRoomChangeRequest request,
        CancellationToken cancellationToken
    )
    {
        var context = await LoadContextAsync(showtimeId, request.TargetRoomId, null, cancellationToken);
        var sourceRoomId = context.Source.RoomId;
        var now = DateTime.UtcNow;
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        ShowtimeRoomChange change;
        Plan plan;

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            var claimed = await db
                .Showtimes.Where(s =>
                    s.ShowtimeId == showtimeId
                    && s.RoomId == sourceRoomId
                    && s.Status == ShowtimeStatus.Scheduled
                    && s.StartTime > now
                )
                .ExecuteUpdateAsync(
                    s => s.SetProperty(x => x.RoomId, request.TargetRoomId),
                    cancellationToken
                );
            if (claimed == 0)
                throw new ConflictAppException(Messages.Conflict_RoomChangeConcurrent);

            plan = await BuildPlanAsync(showtimeId, request, sourceRoomId, cancellationToken);
            if (plan.Issues.Count > 0)
            {
                var first = plan.Issues[0];
                throw first.IsConflict
                    ? new ConflictAppException(first.Message)
                    : new BusinessRuleException(first.Message);
            }

            await RemapSeatsAsync(showtimeId, plan, cancellationToken);

            var seatsMoved = plan.Mappings.Count(m => m.TargetId is not null);
            change = new ShowtimeRoomChange
            {
                ShowtimeId = showtimeId,
                FromRoomId = sourceRoomId,
                ToRoomId = request.TargetRoomId,
                SeatsMoved = seatsMoved,
                ChangedByUserId = actorAccessor.Current?.UserId,
                ChangedAt = now,
                Note = note,
            };
            foreach (var due in plan.Dues)
                change.RefundDues.Add(
                    new RoomChangeRefundDue
                    {
                        OrderId = due.OrderId,
                        OrderLineId = due.OrderLineId,
                        FromSeatId = due.FromSeatId,
                        ToSeatId = due.ToSeatId,
                        AmountDue = due.Amount,
                        Status = RoomChangeRefundStatus.Pending,
                        CreatedAt = now,
                    }
                );
            db.ShowtimeRoomChanges.Add(change);
            await db.SaveChangesAsync(cancellationToken);

            var sourcesById = plan.Sources.ToDictionary(s => s.ShowtimeSeatId);
            auditLogger.Record(
                AuditActions.ChangeRoom,
                AuditEntityTypes.Showtime,
                showtimeId.ToString(),
                AuditSnapshots.ShowtimeRoomBeforeChange(sourceRoomId),
                AuditSnapshots.ShowtimeRoomAfterChange(
                    request.TargetRoomId,
                    seatsMoved,
                    plan.Mappings.Count(m => NeedsReview(m, sourcesById[m.SourceId])),
                    plan.Dues.Count,
                    plan.Dues.Sum(d => d.Amount),
                    note
                )
            );

            await eventDispatcher.PublishAsync(
                new ShowtimeRoomChanged(
                    change.ShowtimeRoomChangeId,
                    showtimeId,
                    sourceRoomId,
                    request.TargetRoomId,
                    now
                ),
                cancellationToken
            );

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        try
        {
            await realtimeNotifier.NotifyShowtimeRoomChangedAsync(
                showtimeId,
                plan.Target.RoomId,
                plan.Target.RoomName,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "ShowtimeRoomChangeNotifyFailed ShowtimeId {ShowtimeId}",
                showtimeId
            );
        }

        logger.LogInformation(
            "ShowtimeRoomChanged ShowtimeId {ShowtimeId} FromRoomId {FromRoomId} ToRoomId {ToRoomId} SeatsMoved {SeatsMoved} RefundDues {RefundDues}",
            showtimeId,
            sourceRoomId,
            request.TargetRoomId,
            change.SeatsMoved,
            plan.Dues.Count
        );

        return new ShowtimeRoomChangeResponse(
            change.ShowtimeRoomChangeId,
            showtimeId,
            sourceRoomId,
            plan.Source.RoomName,
            plan.Target.RoomId,
            plan.Target.RoomName,
            change.SeatsMoved,
            plan.OrderCount,
            plan.Dues.Count,
            plan.Dues.Sum(d => d.Amount)
        );
    }

    private async Task RemapSeatsAsync(int showtimeId, Plan plan, CancellationToken cancellationToken)
    {
        var sourcesById = plan.Sources.ToDictionary(s => s.ShowtimeSeatId);
        var targetsById = plan.Targets.ToDictionary(t => t.SeatId);
        var basePrice = plan.Showtime.BasePrice;

        foreach (var mapping in plan.Mappings.Where(m => m.TargetId is not null))
        {
            var source = sourcesById[mapping.SourceId];
            var target = targetsById[mapping.TargetId!.Value];
            var showtimeSeatId = source.ShowtimeSeatId;
            var targetSeatId = target.SeatId;

            int affected;
            if (source.Status == ShowtimeSeatStatus.Booked)
            {
                affected = await db
                    .ShowtimeSeats.Where(ss =>
                        ss.ShowtimeSeatId == showtimeSeatId && ss.Status == ShowtimeSeatStatus.Booked
                    )
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(x => x.SeatId, targetSeatId),
                        cancellationToken
                    );
            }
            else
            {
                var status = source.Status;
                var heldByOrderId = source.HeldByOrderId;
                var price = basePrice * target.Multiplier;
                affected = await db
                    .ShowtimeSeats.Where(ss =>
                        ss.ShowtimeSeatId == showtimeSeatId
                        && ss.Status == status
                        && ss.HeldByOrderId == heldByOrderId
                    )
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(x => x.SeatId, targetSeatId).SetProperty(x => x.Price, price),
                        cancellationToken
                    );
            }

            if (affected == 0)
                throw new ConflictAppException(Messages.Conflict_RoomChangeConcurrent);
        }

        var dropSources = plan
            .Mappings.Where(m => m.TargetId is null && !sourcesById[m.SourceId].Required)
            .Select(m => sourcesById[m.SourceId])
            .ToList();
        var dropIds = dropSources.Select(s => s.ShowtimeSeatId).ToList();
        if (dropIds.Count > 0)
        {
            var blockedDropIds = dropSources
                .Where(s => s.Status == ShowtimeSeatStatus.Blocked)
                .Select(s => s.ShowtimeSeatId)
                .ToList();
            if (blockedDropIds.Count > 0)
            {
                var droppedBlocks = await db
                    .SeatBlocks.AsNoTracking()
                    .Where(b => blockedDropIds.Contains(b.ShowtimeSeatId))
                    .ToListAsync(cancellationToken);
                foreach (var block in droppedBlocks)
                    auditLogger.Record(
                        AuditActions.Delete,
                        AuditEntityTypes.SeatBlock,
                        block.SeatBlockId.ToString(),
                        AuditSnapshots.SeatBlockState(block),
                        null
                    );
            }

            var deleted = await db
                .ShowtimeSeats.Where(ss =>
                    dropIds.Contains(ss.ShowtimeSeatId)
                    && (
                        ss.Status == ShowtimeSeatStatus.Available
                        || ss.Status == ShowtimeSeatStatus.Blocked
                    )
                )
                .ExecuteDeleteAsync(cancellationToken);
            if (deleted != dropIds.Count)
                throw new ConflictAppException(Messages.Conflict_RoomChangeConcurrent);
        }

        var usedTargets = plan
            .Mappings.Where(m => m.TargetId is not null)
            .Select(m => m.TargetId!.Value)
            .ToHashSet();
        foreach (var target in plan.Targets.Where(t => !usedTargets.Contains(t.SeatId)))
            db.ShowtimeSeats.Add(
                new ShowtimeSeat
                {
                    ShowtimeId = showtimeId,
                    SeatId = target.SeatId,
                    Status = ShowtimeSeatStatus.Available,
                    Price = basePrice * target.Multiplier,
                }
            );
    }

    private async Task<RoomChangeContext> LoadContextAsync(
        int showtimeId,
        int targetRoomId,
        int? expectedSourceRoomId,
        CancellationToken cancellationToken
    )
    {
        var showtime =
            await db
                .Showtimes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Showtime);

        ShowtimeRules.EnsureScheduled(showtime, Messages.Common_ActionChangeRoom);
        if (showtime.StartTime <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_ShowtimeStartedCannotChangeRoom);

        var sourceRoomId = expectedSourceRoomId ?? showtime.RoomId;
        if (targetRoomId == sourceRoomId)
            throw new BusinessRuleException(Messages.Rule_RoomChangeSameRoom);

        var rooms = await db
            .CinemaRooms.AsNoTracking()
            .Where(r => r.RoomId == sourceRoomId || r.RoomId == targetRoomId)
            .ToListAsync(cancellationToken);
        var source =
            rooms.FirstOrDefault(r => r.RoomId == sourceRoomId)
            ?? throw new NotFoundException(Messages.NotFound_Room);
        var target =
            rooms.FirstOrDefault(r => r.RoomId == targetRoomId)
            ?? throw new ValidationAppException(
                Messages.Invalid_RoomIdNotFound,
                new Dictionary<string, string[]>
                {
                    ["targetRoomId"] = [Messages.Invalid_RoomIdNotFound],
                }
            );

        return new RoomChangeContext(showtime, source, target);
    }

    private async Task<Plan> BuildPlanAsync(
        int showtimeId,
        ShowtimeRoomChangeRequest request,
        int? expectedSourceRoomId,
        CancellationToken cancellationToken
    )
    {
        var (showtime, source, target) = await LoadContextAsync(
            showtimeId,
            request.TargetRoomId,
            expectedSourceRoomId,
            cancellationToken
        );
        var now = DateTime.UtcNow;
        var issues = new List<Issue>();

        if (!target.IsActive)
            issues.Add(new Issue(Messages.Rule_RoomInactive, false));
        if (
            await db.HasOverlapAsync(
                target.RoomId,
                showtime.StartTime,
                showtime.EndTime,
                showtimeId,
                cancellationToken
            )
        )
            issues.Add(new Issue(Messages.Conflict_ShowtimeOverlapInRoom, true));
        if (
            await db.HasClosureAsync(
                target.RoomId,
                showtime.StartTime,
                showtime.EndTime,
                cancellationToken
            )
        )
            issues.Add(new Issue(Messages.Conflict_ShowtimeOverlapRoomClosure, true));

        var sources = (
            await db
                .ShowtimeSeats.AsNoTracking()
                .Where(ss => ss.ShowtimeId == showtimeId)
                .Select(ss => new
                {
                    ss.ShowtimeSeatId,
                    ss.SeatId,
                    Row = ss.Seat.SeatRow,
                    Number = ss.Seat.SeatNumber,
                    ss.Seat.SeatTypeId,
                    SeatTypeName = ss.Seat.SeatType != null ? ss.Seat.SeatType.Name : null,
                    IsCouple = ss.Seat.SeatType != null && ss.Seat.SeatType.IsCouple,
                    ss.Seat.CoupleId,
                    Multiplier = ss.Seat.SeatType != null ? ss.Seat.SeatType.PriceMultiplier : 1m,
                    ss.Status,
                    ss.HeldUntil,
                    ss.HeldByOrderId,
                    HasOrders = ss.OrderLines.Any(),
                })
                .ToListAsync(cancellationToken)
        )
            .Select(x => new SourceSeat(
                x.ShowtimeSeatId,
                x.SeatId,
                x.Row,
                x.Number,
                x.SeatTypeId,
                x.SeatTypeName,
                x.IsCouple,
                x.CoupleId,
                x.Multiplier,
                x.Status,
                x.HeldUntil,
                x.HeldByOrderId,
                x.HasOrders
            ))
            .ToList();

        var targets = (
            await db
                .Seats.AsNoTracking()
                .Where(s => s.RoomId == target.RoomId && s.IsActive && !s.IsAisle)
                .Select(s => new
                {
                    s.SeatId,
                    Row = s.SeatRow,
                    Number = s.SeatNumber,
                    s.SeatTypeId,
                    SeatTypeName = s.SeatType != null ? s.SeatType.Name : null,
                    IsCouple = s.SeatType != null && s.SeatType.IsCouple,
                    s.CoupleId,
                    Multiplier = s.SeatType != null ? s.SeatType.PriceMultiplier : 1m,
                })
                .ToListAsync(cancellationToken)
        )
            .Select(x => new TargetSeat(
                x.SeatId,
                x.Row,
                x.Number,
                x.SeatTypeId,
                x.SeatTypeName,
                x.IsCouple,
                x.CoupleId,
                x.Multiplier
            ))
            .ToList();

        if (
            sources.Any(s =>
                s.Status == ShowtimeSeatStatus.Held && (s.HeldUntil == null || s.HeldUntil > now)
            )
        )
            issues.Add(new Issue(Messages.Conflict_RoomChangeHasActiveHolds, true));

        var requiredCount = sources.Count(s => s.Required);
        if (requiredCount > targets.Count)
            issues.Add(
                new Issue(
                    string.Format(Messages.Rule_RoomChangeNotEnoughSeats, requiredCount, targets.Count),
                    false
                )
            );

        var overrides = ValidateOverrides(request, sources, targets);
        var mappings = ShowtimeSeatRemapper.Map(
            [
                .. sources.Select(s => new RemapSourceSeat(
                    s.ShowtimeSeatId,
                    s.Row,
                    s.Number,
                    s.SeatTypeId,
                    s.IsCouple,
                    s.CoupleId,
                    s.Required
                )),
            ],
            [
                .. targets.Select(t => new RemapTargetSeat(
                    t.SeatId,
                    t.Row,
                    t.Number,
                    t.SeatTypeId,
                    t.IsCouple,
                    t.CoupleId
                )),
            ],
            overrides
        );

        var sourcesById = sources.ToDictionary(s => s.ShowtimeSeatId);
        var targetsById = targets.ToDictionary(t => t.SeatId);

        var unmapped = mappings.Count(m => m.TargetId is null && sourcesById[m.SourceId].Required);
        if (unmapped > 0)
            issues.Add(new Issue(string.Format(Messages.Rule_RoomChangeSeatsUnmapped, unmapped), false));

        if (HasBrokenCouples(mappings, sourcesById, targetsById))
            issues.Add(new Issue(Messages.Rule_RoomChangeCoupleBroken, false));

        var (dues, orderCount) = await BuildRefundDuesAsync(
            showtimeId,
            mappings,
            sourcesById,
            targetsById,
            cancellationToken
        );

        return new Plan(
            showtime,
            source,
            target,
            sources,
            targets,
            mappings,
            issues,
            dues,
            orderCount
        );
    }

    private static Dictionary<int, int> ValidateOverrides(
        ShowtimeRoomChangeRequest request,
        IReadOnlyList<SourceSeat> sources,
        IReadOnlyList<TargetSeat> targets
    )
    {
        var result = new Dictionary<int, int>();
        if (request.Overrides is null)
            return result;

        var sourceIds = sources.Select(s => s.ShowtimeSeatId).ToHashSet();
        var targetIds = targets.Select(t => t.SeatId).ToHashSet();
        var usedTargets = new HashSet<int>();
        foreach (var item in request.Overrides)
        {
            if (
                !sourceIds.Contains(item.ShowtimeSeatId)
                || !targetIds.Contains(item.TargetSeatId)
                || !usedTargets.Add(item.TargetSeatId)
                || !result.TryAdd(item.ShowtimeSeatId, item.TargetSeatId)
            )
                throw new ValidationAppException(
                    Messages.Validation_RoomChange_InvalidMapping,
                    new Dictionary<string, string[]>
                    {
                        ["overrides"] = [Messages.Validation_RoomChange_InvalidMapping],
                    }
                );
        }

        return result;
    }

    private static bool HasBrokenCouples(
        IReadOnlyList<RemapResult> mappings,
        IReadOnlyDictionary<int, SourceSeat> sourcesById,
        IReadOnlyDictionary<int, TargetSeat> targetsById
    )
    {
        var mappedRequired = mappings
            .Where(m => m.TargetId is not null && sourcesById[m.SourceId].Required)
            .Select(m => (Source: sourcesById[m.SourceId], Target: targetsById[m.TargetId!.Value]))
            .ToList();

        if (mappedRequired.Any(x => x.Source.IsCouple != x.Target.IsCouple))
            return true;

        return mappedRequired
            .Where(x => x.Source.IsCouple && x.Source.CoupleId is not null)
            .GroupBy(x => x.Source.CoupleId!.Value)
            .Any(g => g.Select(x => x.Target.CoupleId).Distinct().Count() != 1);
    }

    private async Task<(List<RefundDueRow> Dues, int OrderCount)> BuildRefundDuesAsync(
        int showtimeId,
        IReadOnlyList<RemapResult> mappings,
        IReadOnlyDictionary<int, SourceSeat> sourcesById,
        IReadOnlyDictionary<int, TargetSeat> targetsById,
        CancellationToken cancellationToken
    )
    {
        var lines = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && l.ShowtimeId == showtimeId
                && l.ShowtimeSeatId != null
                && l.Order.Status == OrderStatus.Confirmed
                && l.Quantity > l.RefundedQuantity
            )
            .Select(l => new
            {
                l.OrderLineId,
                l.OrderId,
                ShowtimeSeatId = l.ShowtimeSeatId!.Value,
                l.PaidAmount,
                l.RefundedAmount,
            })
            .ToListAsync(cancellationToken);

        var targetBySource = mappings
            .Where(m => m.TargetId is not null)
            .ToDictionary(m => m.SourceId, m => m.TargetId!.Value);

        var dues = new List<RefundDueRow>();
        foreach (var line in lines)
        {
            if (
                !targetBySource.TryGetValue(line.ShowtimeSeatId, out var targetSeatId)
                || !sourcesById.TryGetValue(line.ShowtimeSeatId, out var source)
            )
                continue;

            var target = targetsById[targetSeatId];
            var remaining = line.PaidAmount - line.RefundedAmount;
            if (target.Multiplier >= source.Multiplier || source.Multiplier <= 0m || remaining <= 0m)
                continue;

            var amount = OrderPricing.RoundToDong(
                remaining * (source.Multiplier - target.Multiplier) / source.Multiplier
            );
            if (amount > 0m)
                dues.Add(
                    new RefundDueRow(
                        line.OrderId,
                        line.OrderLineId,
                        line.ShowtimeSeatId,
                        source.SeatId,
                        target.SeatId,
                        amount
                    )
                );
        }

        return (dues, lines.Select(l => l.OrderId).Distinct().Count());
    }

    private static bool NeedsReview(RemapResult mapping, SourceSeat source) =>
        mapping.Kind == SeatMatchKind.Nearest
        || (mapping.Kind == SeatMatchKind.Unmapped && source.Required);

    private static RoomChangeSeatRefDto ToRef(
        string row,
        int number,
        int? seatTypeId,
        string? seatType,
        bool isCouple
    ) => new(row, number, seatTypeId, seatType, isCouple);

    private sealed record RoomChangeContext(Showtime Showtime, CinemaRoom Source, CinemaRoom Target);

    private sealed record SourceSeat(
        int ShowtimeSeatId,
        int SeatId,
        string Row,
        int Number,
        int? SeatTypeId,
        string? SeatTypeName,
        bool IsCouple,
        Guid? CoupleId,
        decimal Multiplier,
        ShowtimeSeatStatus Status,
        DateTime? HeldUntil,
        int? HeldByOrderId,
        bool HasOrders
    )
    {
        public bool Required =>
            (Status != ShowtimeSeatStatus.Available && Status != ShowtimeSeatStatus.Blocked)
            || HasOrders;
    }

    private sealed record TargetSeat(
        int SeatId,
        string Row,
        int Number,
        int? SeatTypeId,
        string? SeatTypeName,
        bool IsCouple,
        Guid? CoupleId,
        decimal Multiplier
    );

    private sealed record Issue(string Message, bool IsConflict);

    private sealed record RefundDueRow(
        int OrderId,
        int OrderLineId,
        int ShowtimeSeatId,
        int FromSeatId,
        int ToSeatId,
        decimal Amount
    );

    private sealed record Plan(
        Showtime Showtime,
        CinemaRoom Source,
        CinemaRoom Target,
        List<SourceSeat> Sources,
        List<TargetSeat> Targets,
        List<RemapResult> Mappings,
        List<Issue> Issues,
        List<RefundDueRow> Dues,
        int OrderCount
    );
}
