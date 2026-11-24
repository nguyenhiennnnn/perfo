using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Rooms;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Rooms;

public class RoomClosureService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor
) : IRoomClosureService
{
    public async Task<OffsetPageResult<RoomClosureDto>> GetAdminAsync(
        RoomClosureListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.RoomClosures.AsNoTracking();
        if (filter.RoomId is { } roomId)
            query = query.Where(c => c.RoomId == roomId);
        if (filter.From is { } from)
            query = query.Where(c => c.EndTime > from);
        if (filter.To is { } to)
            query = query.Where(c => c.StartTime < to);
        if (filter.Ongoing is { } ongoing)
        {
            var now = DateTime.UtcNow;
            query = ongoing
                ? query.Where(c => c.EndTime > now)
                : query.Where(c => c.EndTime <= now);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.StartTime)
            .ThenByDescending(c => c.RoomClosureId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new RoomClosureDto(
                c.RoomClosureId,
                c.RoomId,
                c.Room.RoomName,
                c.StartTime,
                c.EndTime,
                c.Reason,
                c.CreatedByUserId,
                c.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return OffsetPageResult<RoomClosureDto>.Create(items, page, pageSize, total);
    }

    public async Task<RoomClosureDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var closure = await FindAsync(id, tracking: false, cancellationToken);
        return ToDto(closure);
    }

    public async Task<RoomClosureDto> CreateAsync(
        CreateRoomClosureRequest request,
        CancellationToken cancellationToken
    )
    {
        var room =
            await db
                .CinemaRooms.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoomId == request.RoomId, cancellationToken)
            ?? throw new ValidationAppException(
                Messages.Invalid_RoomIdNotFound,
                new Dictionary<string, string[]> { ["roomId"] = [Messages.Invalid_RoomIdNotFound] }
            );

        if (request.EndTime <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_RoomClosureMustEndInFuture);

        var overlapped = await db.RoomClosures.AnyAsync(
            c =>
                c.RoomId == request.RoomId
                && c.EndTime > request.StartTime
                && c.StartTime < request.EndTime,
            cancellationToken
        );
        if (overlapped)
            throw new ConflictAppException(Messages.Conflict_RoomClosureOverlap);

        var closure = new RoomClosure
        {
            RoomId = request.RoomId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Reason = request.Reason.Trim(),
            CreatedByUserId = actorAccessor.Current?.UserId,
            CreatedAt = DateTime.UtcNow,
            Room = room,
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.RoomClosures.Add(closure);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.RoomClosure,
            closure.RoomClosureId.ToString(),
            null,
            AuditSnapshots.RoomClosureState(closure)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToDto(closure);
    }

    public async Task<DeleteRoomClosureResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var closure = await FindAsync(id, tracking: true, cancellationToken);
        var now = DateTime.UtcNow;
        if (closure.EndTime <= now)
            throw new BusinessRuleException(Messages.Rule_RoomClosureAlreadyEnded);

        var before = AuditSnapshots.RoomClosureState(closure);
        bool deleted;
        if (closure.StartTime > now)
        {
            db.RoomClosures.Remove(closure);
            auditLogger.Record(
                AuditActions.Delete,
                AuditEntityTypes.RoomClosure,
                closure.RoomClosureId.ToString(),
                before,
                null
            );
            deleted = true;
        }
        else
        {
            closure.EndTime = now;
            auditLogger.Record(
                AuditActions.Update,
                AuditEntityTypes.RoomClosure,
                closure.RoomClosureId.ToString(),
                before,
                AuditSnapshots.RoomClosureState(closure)
            );
            deleted = false;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new DeleteRoomClosureResponse(id, deleted, !deleted);
    }

    public async Task<AffectedShowtimesResponse> GetClosureAffectedShowtimesAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var closure = await FindAsync(id, tracking: false, cancellationToken);
        return await GetAffectedShowtimesAsync(
            closure.RoomId,
            closure.StartTime,
            closure.EndTime,
            cancellationToken
        );
    }

    public async Task<AffectedShowtimesResponse> GetAffectedShowtimesAsync(
        int roomId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken
    )
    {
        if (endTime <= startTime)
            throw new ValidationAppException(Messages.Validation_RoomClosure_RangeOrder);

        var roomName =
            await db
                .CinemaRooms.AsNoTracking()
                .Where(r => r.RoomId == roomId)
                .Select(r => r.RoomName)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Room);

        var now = DateTime.UtcNow;
        var showtimes = await db
            .Showtimes.AsNoTracking()
            .Where(s =>
                s.RoomId == roomId
                && s.Status == ShowtimeStatus.Scheduled
                && s.EndTime > now
                && s.EndTime > startTime
                && s.StartTime < endTime
            )
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.ShowtimeId)
            .Select(s => new
            {
                s.ShowtimeId,
                s.MovieId,
                MovieTitle = s.Movie.Title,
                s.StartTime,
                s.EndTime,
            })
            .ToListAsync(cancellationToken);

        var showtimeIds = showtimes.Select(s => s.ShowtimeId).ToList();
        var tickets = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && l.ShowtimeId != null
                && showtimeIds.Contains(l.ShowtimeId.Value)
                && l.Order.Status == OrderStatus.Confirmed
                && l.Quantity > l.RefundedQuantity
            )
            .Select(l => new
            {
                ShowtimeId = l.ShowtimeId!.Value,
                l.OrderId,
                l.Order.UserId,
                l.Order.CustomerPhone,
                Tickets = l.Quantity - l.RefundedQuantity,
            })
            .ToListAsync(cancellationToken);

        var byShowtime = tickets
            .Select(t => new
            {
                t.ShowtimeId,
                t.OrderId,
                t.Tickets,
                Customer = CustomerKey(t.UserId, t.CustomerPhone, t.OrderId),
            })
            .ToLookup(t => t.ShowtimeId);

        var items = showtimes
            .Select(s =>
            {
                var rows = byShowtime[s.ShowtimeId].ToList();
                return new AffectedShowtimeDto(
                    s.ShowtimeId,
                    s.MovieId,
                    s.MovieTitle,
                    roomId,
                    roomName,
                    s.StartTime,
                    s.EndTime,
                    rows.Select(r => r.OrderId).Distinct().Count(),
                    rows.Select(r => r.Customer).Distinct().Count(),
                    rows.Sum(r => r.Tickets)
                );
            })
            .ToList();

        var allRows = tickets
            .Select(t => new
            {
                t.OrderId,
                Customer = CustomerKey(t.UserId, t.CustomerPhone, t.OrderId),
            })
            .ToList();

        return new AffectedShowtimesResponse(
            roomId,
            startTime,
            endTime,
            items,
            items.Count,
            allRows.Select(r => r.OrderId).Distinct().Count(),
            allRows.Select(r => r.Customer).Distinct().Count(),
            items.Sum(i => i.TicketCount)
        );
    }

    private static string CustomerKey(int? userId, string? phone, int orderId) =>
        userId is { } id ? $"u:{id}"
        : !string.IsNullOrWhiteSpace(phone) ? $"p:{phone.Trim()}"
        : $"o:{orderId}";

    private async Task<RoomClosure> FindAsync(
        int id,
        bool tracking,
        CancellationToken cancellationToken
    )
    {
        var query = tracking ? db.RoomClosures : db.RoomClosures.AsNoTracking();
        return await query
                .Include(c => c.Room)
                .FirstOrDefaultAsync(c => c.RoomClosureId == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_RoomClosure);
    }

    private static RoomClosureDto ToDto(RoomClosure c) =>
        new(
            c.RoomClosureId,
            c.RoomId,
            c.Room.RoomName,
            c.StartTime,
            c.EndTime,
            c.Reason,
            c.CreatedByUserId,
            c.CreatedAt
        );
}
