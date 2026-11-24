using System.Text.Json;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.DailyClosings;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.DailyClosings;

public class DailyClosingService(
    IAppDbContext db,
    IAppCache cache,
    IAuditLogger auditLogger,
    IDailyClosingReportService reportService
) : IDailyClosingService
{
    public async Task<OffsetPageResult<DailyClosingListItemDto>> GetClosingsAsync(
        DailyClosingListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.DailyClosings.AsNoTracking().Where(c => c.Status == DailyClosingStatus.Closed);
        if (filter.FromDate is { } from)
            query = query.Where(c => c.BusinessDate >= from);
        if (filter.ToDate is { } to)
            query = query.Where(c => c.BusinessDate <= to);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(c => c.BusinessDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new { Closing = c, ClosedByName = c.ClosedByUser!.FullName })
            .ToListAsync(cancellationToken);

        return OffsetPageResult<DailyClosingListItemDto>.Create(
            rows.Select(r => ToItem(r.Closing, r.ClosedByName)).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<DailyClosingDto> GetByDateAsync(
        DateOnly businessDate,
        CancellationToken cancellationToken
    )
    {
        var row = await db
            .DailyClosings.AsNoTracking()
            .Where(c => c.BusinessDate == businessDate && c.Status == DailyClosingStatus.Closed)
            .Select(c => new { Closing = c, ClosedByName = c.ClosedByUser!.FullName })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_DailyClosing);

        return ToDto(row.Closing, row.ClosedByName);
    }

    public async Task<DailyClosingDto> CloseAsync(
        int userId,
        DateOnly businessDate,
        CloseDailyClosingRequest request,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var today = BusinessDay.Of(DateTime.UtcNow, settings.OpeningTime, settings.ClosingTime);
        if (businessDate == default || businessDate >= today)
            throw new BusinessRuleException(Messages.Rule_DailyClosingDayNotEnded);

        var existing = await db
            .DailyClosings.AsNoTracking()
            .Where(c => c.BusinessDate == businessDate)
            .Select(c => new { c.DailyClosingId, c.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is { Status: DailyClosingStatus.Closed })
            throw new ConflictAppException(Messages.Conflict_DailyClosingAlreadyClosed);

        var snapshot = await reportService.BuildSnapshotAsync(businessDate, cancellationToken);

        var endUtc = BusinessDay.EndUtc(businessDate, settings.OpeningTime, settings.ClosingTime);
        var openShifts = await db
            .CounterShifts.AsNoTracking()
            .CountAsync(
                s => s.Status == CounterShiftStatus.Open && s.OpenedAt < endUtc,
                cancellationToken
            );
        var problemOrders = snapshot.Orders.ProblemOrders;
        var overridden = EnsureCanClose(settings, request, openShifts, problemOrders);

        var closedAt = DateTime.UtcNow;
        var expected = OrderPricing.RoundToDong(snapshot.Cash.ExpectedCash);
        var counted = OrderPricing.RoundToDong(request.CountedCash);
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        var closing = new DailyClosing
        {
            BusinessDate = businessDate,
            Status = DailyClosingStatus.Closed,
            SnapshotJson = AuditJson.Serialize(snapshot),
            ExpectedCash = expected,
            CountedCash = counted,
            Variance = counted - expected,
            Note = note,
            ClosedByUserId = userId,
            ClosedAt = closedAt,
        };

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            db.DailyClosings.Add(closing);
            await db.SaveChangesAsync(cancellationToken);
            auditLogger.Record(
                AuditActions.CloseDay,
                AuditEntityTypes.DailyClosing,
                closing.DailyClosingId.ToString(),
                null,
                AuditJson.ToObject(
                    new
                    {
                        closing.BusinessDate,
                        closing.ExpectedCash,
                        closing.CountedCash,
                        closing.Variance,
                        closing.Note,
                        Overridden = overridden,
                        OpenShifts = openShifts,
                        ProblemOrders = problemOrders,
                    }
                )
            );
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (
                await db
                    .DailyClosings.AsNoTracking()
                    .AnyAsync(c => c.BusinessDate == businessDate, cancellationToken)
            )
                throw new ConflictAppException(Messages.Conflict_DailyClosingAlreadyClosed);
            throw;
        }

        var closedByName = await db
            .Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        return ToDto(closing, closedByName);
    }

    private static bool EnsureCanClose(
        CinemaSettingsSnapshot settings,
        CloseDailyClosingRequest request,
        int openShifts,
        int problemOrders
    )
    {
        if (openShifts == 0 && problemOrders == 0)
            return false;

        if (!request.OverrideBlockers)
        {
            var reasons = new List<string>();
            if (openShifts > 0)
                reasons.Add(string.Format(Messages.Rule_DailyClosingOpenShifts, openShifts));
            if (problemOrders > 0)
                reasons.Add(string.Format(Messages.Rule_DailyClosingProblemOrders, problemOrders));
            throw new BusinessRuleException(string.Join(" ", reasons));
        }

        if (!settings.DailyClosingAllowOverride)
            throw new BusinessRuleException(Messages.Rule_DailyClosingOverrideNotAllowed);
        if (string.IsNullOrWhiteSpace(request.Note))
            throw new BusinessRuleException(Messages.Rule_DailyClosingOverrideNoteRequired);

        return true;
    }

    private static DailyClosingListItemDto ToItem(DailyClosing c, string closedByName) =>
        new(
            c.DailyClosingId,
            c.BusinessDate,
            c.Status.ToString(),
            c.ExpectedCash ?? 0m,
            c.CountedCash ?? 0m,
            c.Variance ?? 0m,
            c.Note,
            c.ClosedByUserId ?? 0,
            closedByName,
            c.ClosedAt ?? default
        );

    private static DailyClosingDto ToDto(DailyClosing c, string closedByName)
    {
        using var document = JsonDocument.Parse(c.SnapshotJson ?? "{}");
        return new DailyClosingDto(ToItem(c, closedByName), document.RootElement.Clone());
    }
}
