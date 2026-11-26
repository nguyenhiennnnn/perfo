using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Services.Audit;
using Cinema.Application.Services.Pricing;
using Cinema.Application.Utils;
using Cinema.Application.Validators.Pricing;
using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalRateService(IAppDbContext db, IAppCache cache, IAuditLogger auditLogger)
    : IRoomRentalRateService
{
    public async Task<OffsetPageResult<RoomRentalRateDto>> GetAdminAsync(
        int page,
        int pageSize,
        RoomRentalRateListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.RoomRentalRates.AsNoTracking().Include(r => r.Room).AsQueryable();
        if (filter.RoomId is { } roomId)
            query = query.Where(r => r.RoomId == roomId);
        if (filter.IsActive is { } isActive)
            query = query.Where(r => r.IsActive == isActive);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(r =>
                    EF.Functions.ILike(r.Name, pattern)
                    || EF.Functions.ILike(r.Room!.RoomName, pattern)
                )
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.RoomId)
            .ThenBy(r => r.StartTime)
            .ThenBy(r => r.MinDurationMinutes)
            .ThenBy(r => r.RoomRentalRateId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<RoomRentalRateDto>.Create(
            items.Select(ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<RoomRentalRateDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        ToDto(
            await db
                .RoomRentalRates.AsNoTracking()
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.RoomRentalRateId == id, cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_RoomRentalRate)
        );

    public async Task<RoomRentalRateDto> CreateAsync(
        UpsertRoomRentalRateRequest request,
        CancellationToken cancellationToken
    )
    {
        var room = await FindRoomAsync(request.RoomId, cancellationToken);
        await EnsureNoOverlapAsync(0, request, cancellationToken);

        var entity = new RoomRentalRate { CreatedAt = DateTime.UtcNow };
        Apply(entity, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.RoomRentalRates.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.RoomRentalRate,
            entity.RoomRentalRateId.ToString(),
            null,
            AuditSnapshots.RoomRentalRateState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.RoomRentalRates, cancellationToken);
        entity.Room = room;
        return ToDto(entity);
    }

    public async Task<RoomRentalRateDto> UpdateAsync(
        int id,
        UpsertRoomRentalRateRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        var room = await FindRoomAsync(request.RoomId, cancellationToken);
        await EnsureNoOverlapAsync(id, request, cancellationToken);

        var before = AuditSnapshots.RoomRentalRateState(entity);
        Apply(entity, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.RoomRentalRate,
            entity.RoomRentalRateId.ToString(),
            before,
            AuditSnapshots.RoomRentalRateState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.RoomRentalRates, cancellationToken);
        entity.Room = room;
        return ToDto(entity);
    }

    public async Task<DeletePricingEntityResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);

        var before = AuditSnapshots.RoomRentalRateState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.RoomRentalRate,
            entity.RoomRentalRateId.ToString(),
            before,
            AuditSnapshots.RoomRentalRateState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.RoomRentalRates, cancellationToken);
        return new DeletePricingEntityResponse(id, true);
    }

    private async Task<RoomRentalRate> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.RoomRentalRates.FirstOrDefaultAsync(
            r => r.RoomRentalRateId == id,
            cancellationToken
        ) ?? throw new NotFoundException(Messages.NotFound_RoomRentalRate);

    private async Task<CinemaRoom> FindRoomAsync(int roomId, CancellationToken cancellationToken) =>
        await db.CinemaRooms.FirstOrDefaultAsync(r => r.RoomId == roomId, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_Room);

    private async Task EnsureNoOverlapAsync(
        int currentId,
        UpsertRoomRentalRateRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!request.IsActive)
            return;

        var start = PricingValidation.ParseTime(request.StartTime);
        var end = PricingValidation.ParseTime(request.EndTime);
        var overlapped = await db.RoomRentalRates.AnyAsync(
            r =>
                r.RoomRentalRateId != currentId
                && r.IsActive
                && r.RoomId == request.RoomId
                && r.StartTime < end
                && r.EndTime > start
                && r.MinDurationMinutes <= request.MaxDurationMinutes
                && r.MaxDurationMinutes >= request.MinDurationMinutes,
            cancellationToken
        );
        if (overlapped)
            throw new ConflictAppException(Messages.Conflict_RoomRentalRateOverlap);
    }

    private static void Apply(RoomRentalRate entity, UpsertRoomRentalRateRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.RoomId = request.RoomId;
        entity.StartTime = PricingValidation.ParseTime(request.StartTime);
        entity.EndTime = PricingValidation.ParseTime(request.EndTime);
        entity.MinDurationMinutes = request.MinDurationMinutes;
        entity.MaxDurationMinutes = request.MaxDurationMinutes;
        entity.BasePrice = request.BasePrice;
        entity.PerPersonFee = request.PerPersonFee;
        entity.IncludedAttendees = request.IncludedAttendees;
        entity.IsActive = request.IsActive;
    }

    private static RoomRentalRateDto ToDto(RoomRentalRate r) =>
        new(
            r.RoomRentalRateId,
            r.Name,
            r.RoomId,
            r.Room?.RoomName ?? string.Empty,
            PricingMapper.FormatTime(r.StartTime)!,
            PricingMapper.FormatTime(r.EndTime)!,
            r.MinDurationMinutes,
            r.MaxDurationMinutes,
            r.BasePrice,
            r.PerPersonFee,
            r.IncludedAttendees,
            r.IsActive,
            r.CreatedAt,
            r.UpdatedAt
        );
}
