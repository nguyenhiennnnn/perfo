using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.CounterShifts;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.CounterShifts;

public class CounterShiftService(IAppDbContext db, IAuditLogger auditLogger, IAppCache cache)
    : ICounterShiftService
{
    public async Task<IReadOnlyList<OpenCounterShiftItemDto>> GetOpenShiftsAsync(
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var overdueBefore = BusinessDay.CurrentStartUtc(
            DateTime.UtcNow,
            settings.OpeningTime,
            settings.ClosingTime
        );

        var rows = await db
            .CounterShifts.AsNoTracking()
            .Where(s => s.Status == CounterShiftStatus.Open)
            .OrderBy(s => s.OpenedAt)
            .ThenBy(s => s.CounterShiftId)
            .Select(s => new
            {
                Shift = s,
                EmployeeName = s.Employee.User.FullName,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new OpenCounterShiftItemDto(
                ToDto(r.Shift, r.EmployeeName),
                r.Shift.OpenedAt < overdueBefore,
                r.Shift.UnclosedAlertedAt
            ))
            .ToList();
    }

    public async Task<OffsetPageResult<CounterShiftDto>> GetShiftsAsync(
        CounterShiftActor actor,
        CounterShiftListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.CounterShifts.AsNoTracking();

        if (actor.IsAdmin)
        {
            if (filter.EmployeeId is { } employeeId)
                query = query.Where(s => s.EmployeeId == employeeId);
        }
        else
        {
            var ownEmployeeId = actor.EmployeeId ?? 0;
            query = query.Where(s => s.EmployeeId == ownEmployeeId);
        }

        if (
            !string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<CounterShiftStatus>(filter.Status.Trim(), true, out var status)
        )
            query = query.Where(s => s.Status == status);

        if (filter.FromDate is not null)
            query = query.Where(s =>
                s.OpenedAt >= DateTimeUtils.GetStartOfDay(filter.FromDate.Value)
            );
        if (filter.ToDate is not null)
            query = query.Where(s =>
                s.OpenedAt < DateTimeUtils.GetStartOfNextDay(filter.ToDate.Value)
            );

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(s => s.Status == CounterShiftStatus.Open ? 0 : 1)
            .ThenByDescending(s => s.OpenedAt)
            .ThenByDescending(s => s.CounterShiftId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                Shift = s,
                EmployeeName = s.Employee.User.FullName,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => ToDto(r.Shift, r.EmployeeName)).ToList();
        return OffsetPageResult<CounterShiftDto>.Create(items, page, pageSize, total);
    }

    public async Task<CounterShiftDto?> GetCurrentAsync(
        CounterShiftActor actor,
        CancellationToken cancellationToken
    )
    {
        var row = await db
            .CounterShifts.AsNoTracking()
            .Where(s => s.Status == CounterShiftStatus.Open && s.Employee.UserId == actor.UserId)
            .Select(s => new { Shift = s, EmployeeName = s.Employee.User.FullName })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToDto(row.Shift, row.EmployeeName);
    }

    public async Task<CounterShiftDto> GetByIdAsync(
        CounterShiftActor actor,
        int id,
        CancellationToken cancellationToken
    )
    {
        var shift = await FindAsync(id, tracking: false, cancellationToken);
        EnsureCanAccess(actor, shift);
        return ToDto(shift, await LoadEmployeeNameAsync(shift.EmployeeId, cancellationToken));
    }

    public async Task<CounterShiftDto> OpenAsync(
        CounterShiftActor actor,
        OpenCounterShiftRequest request,
        CancellationToken cancellationToken
    )
    {
        var employeeId =
            actor.EmployeeId
            ?? throw new BusinessRuleException(Messages.Rule_NoEmployeeProfileForCounterShift);

        if (await HasOpenShiftAsync(employeeId, cancellationToken))
            throw new ConflictAppException(Messages.Conflict_CounterShiftAlreadyOpen);

        var shift = new CounterShift
        {
            EmployeeId = employeeId,
            OpenedAt = DateTime.UtcNow,
            OpeningCash = OrderPricing.RoundToDong(request.OpeningCash),
            Status = CounterShiftStatus.Open,
        };

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                cancellationToken
            );
            db.CounterShifts.Add(shift);
            await db.SaveChangesAsync(cancellationToken);
            auditLogger.Record(
                AuditActions.OpenShift,
                AuditEntityTypes.CounterShift,
                shift.CounterShiftId.ToString(),
                null,
                AuditSnapshots.CounterShiftState(shift)
            );
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await HasOpenShiftAsync(employeeId, cancellationToken))
                throw new ConflictAppException(Messages.Conflict_CounterShiftAlreadyOpen);
            throw;
        }

        return ToDto(shift, await LoadEmployeeNameAsync(employeeId, cancellationToken));
    }

    public async Task<CounterShiftDto> CloseAsync(
        CounterShiftActor actor,
        int id,
        CloseCounterShiftRequest request,
        CancellationToken cancellationToken
    )
    {
        var shift = await FindAsync(id, tracking: true, cancellationToken);
        EnsureCanAccess(actor, shift);
        return await CloseShiftAsync(
            shift,
            request.CountedCash,
            request.Note,
            AuditActions.CloseShift,
            cancellationToken
        );
    }

    public async Task<CounterShiftDto> CloseByAdminAsync(
        CounterShiftActor actor,
        int id,
        AdminCloseCounterShiftRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!actor.IsAdmin)
            throw new ForbiddenAppException(Messages.Forbidden_NotCounterShiftOwner);

        var shift = await FindAsync(id, tracking: true, cancellationToken);
        return await CloseShiftAsync(
            shift,
            request.CountedCash,
            request.Note,
            AuditActions.AdminCloseShift,
            cancellationToken
        );
    }

    private async Task<CounterShiftDto> CloseShiftAsync(
        CounterShift shift,
        decimal countedCash,
        string? note,
        string closeAction,
        CancellationToken cancellationToken
    )
    {
        if (shift.Status != CounterShiftStatus.Open)
            throw new ConflictAppException(Messages.Conflict_CounterShiftAlreadyClosed);

        var totals = await db.LoadCounterShiftCashTotalsAsync(shift.CounterShiftId, cancellationToken);
        var before = AuditSnapshots.CounterShiftState(shift);

        shift.ClosedAt = DateTime.UtcNow;
        shift.CountedCash = OrderPricing.RoundToDong(countedCash);
        shift.ExpectedCash = OrderPricing.RoundToDong(totals.Expected(shift.OpeningCash));
        shift.Variance = shift.CountedCash - shift.ExpectedCash;
        shift.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        shift.Status = CounterShiftStatus.Closed;

        auditLogger.Record(
            closeAction,
            AuditEntityTypes.CounterShift,
            shift.CounterShiftId.ToString(),
            before,
            AuditSnapshots.CounterShiftState(shift)
        );
        if (shift.Variance != 0m)
            auditLogger.Record(
                AuditActions.ShiftVariance,
                AuditEntityTypes.CounterShift,
                shift.CounterShiftId.ToString(),
                null,
                AuditJson.ToObject(
                    new
                    {
                        shift.EmployeeId,
                        shift.ExpectedCash,
                        shift.CountedCash,
                        shift.Variance,
                        shift.Note,
                    }
                )
            );

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var stillOpen = await db
                .CounterShifts.AsNoTracking()
                .AnyAsync(
                    s => s.CounterShiftId == shift.CounterShiftId && s.Status == CounterShiftStatus.Open,
                    cancellationToken
                );
            throw new ConflictAppException(
                stillOpen
                    ? Messages.Conflict_CounterShiftConcurrentUpdate
                    : Messages.Conflict_CounterShiftAlreadyClosed
            );
        }

        return ToDto(shift, await LoadEmployeeNameAsync(shift.EmployeeId, cancellationToken));
    }

    private static void EnsureCanAccess(CounterShiftActor actor, CounterShift shift)
    {
        if (actor.IsAdmin)
            return;
        if (actor.EmployeeId is null || actor.EmployeeId != shift.EmployeeId)
            throw new ForbiddenAppException(Messages.Forbidden_NotCounterShiftOwner);
    }

    private Task<bool> HasOpenShiftAsync(int employeeId, CancellationToken cancellationToken) =>
        db.CounterShifts.AsNoTracking()
            .AnyAsync(
                s => s.EmployeeId == employeeId && s.Status == CounterShiftStatus.Open,
                cancellationToken
            );

    private async Task<CounterShift> FindAsync(
        int id,
        bool tracking,
        CancellationToken cancellationToken
    )
    {
        var query = tracking ? db.CounterShifts : db.CounterShifts.AsNoTracking();
        return await query.FirstOrDefaultAsync(s => s.CounterShiftId == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_CounterShift);
    }

    private async Task<string> LoadEmployeeNameAsync(
        int employeeId,
        CancellationToken cancellationToken
    ) =>
        await db
            .Employees.AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .Select(e => e.User.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

    private static CounterShiftDto ToDto(CounterShift s, string employeeName) =>
        new(
            s.CounterShiftId,
            s.EmployeeId,
            employeeName,
            s.OpenedAt,
            s.OpeningCash,
            s.ClosedAt,
            s.CountedCash,
            s.ExpectedCash,
            s.Variance,
            s.Note,
            s.Status.ToString()
        );
}
