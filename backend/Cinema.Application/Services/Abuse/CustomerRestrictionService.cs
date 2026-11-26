using Cinema.Application.Caching;
using Cinema.Application.DTOs.Abuse;
using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces.Abuse;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Abuse;

public class CustomerRestrictionService(
    IAppDbContext db,
    IAppCache cache,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor,
    ILogger<CustomerRestrictionService> logger
) : ICustomerRestrictionService
{
    private const int LockClassId = 20;

    public async Task<ActiveCustomerRestriction?> SyncAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var now = DateTime.UtcNow;

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({LockClassId}, {userId})",
            cancellationToken
        );

        await db
            .CustomerRestrictions.Where(r =>
                r.UserId == userId
                && r.Status == CustomerRestrictionStatus.Active
                && r.ExpiresAt <= now
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(r => r.Status, CustomerRestrictionStatus.Expired),
                cancellationToken
            );

        var active = await db
            .CustomerRestrictions.AsNoTracking()
            .Where(r => r.UserId == userId && r.Status == CustomerRestrictionStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
        if (active is not null)
            return ToActive(active);

        var liftedAt = await db
            .CustomerRestrictions.AsNoTracking()
            .Where(r =>
                r.UserId == userId
                && r.Reason == CustomerRestrictionReason.ExpiredOrderStreak
                && r.Status == CustomerRestrictionStatus.Lifted
            )
            .MaxAsync(r => r.LiftedAt, cancellationToken);

        var streak = ExpiredStreakQuery(userId);
        if (liftedAt is { } lifted)
            streak = streak.Where(o => o.ExpiresAt > lifted);

        var count = await streak.CountAsync(cancellationToken);
        if (count < settings.ExpiredOrderStreakThreshold)
            return null;

        var lastExpiredAt = await streak.MaxAsync(o => o.ExpiresAt, cancellationToken);
        if (lastExpiredAt is not { } last)
            return null;

        var until = last.AddMinutes(settings.ExpiredOrderRestrictionMinutes);
        if (until <= now)
            return null;

        var restriction = new CustomerRestriction
        {
            UserId = userId,
            Reason = CustomerRestrictionReason.ExpiredOrderStreak,
            Status = CustomerRestrictionStatus.Active,
            ExpiredOrderCount = count,
            StartedAt = now,
            ExpiresAt = until,
        };
        db.CustomerRestrictions.Add(restriction);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.RestrictCustomer,
            AuditEntityTypes.CustomerRestriction,
            restriction.CustomerRestrictionId.ToString(),
            null,
            AuditSnapshots.CustomerRestrictionState(restriction)
        );
        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning(
            "CustomerRestricted UserId {UserId} Reason {Reason} ExpiredOrders {Count} Until {Until}",
            userId,
            restriction.Reason,
            count,
            until
        );

        return ToActive(restriction);
    }

    public async Task<OffsetPageResult<CustomerRestrictionDto>> GetAdminAsync(
        CustomerRestrictionListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var now = DateTime.UtcNow;
        await db
            .CustomerRestrictions.Where(r =>
                r.Status == CustomerRestrictionStatus.Active && r.ExpiresAt <= now
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(r => r.Status, CustomerRestrictionStatus.Expired),
                cancellationToken
            );

        var query = db.CustomerRestrictions.AsNoTracking();
        if (filter.Status is { } status)
            query = query.Where(r => r.Status == status);

        query = query.ApplySearch(
            filter.Search,
            (q, text, phone) =>
                q.Where(r =>
                    EF.Functions.ILike(
                        EF.Functions.Unaccent(r.User.FullName),
                        EF.Functions.Unaccent(text)
                    )
                    || EF.Functions.ILike(r.User.Email, text)
                    || (r.User.Phone != null && EF.Functions.ILike(r.User.Phone, phone))
                )
        );

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(r => r.Status == CustomerRestrictionStatus.Active)
            .ThenByDescending(r => r.StartedAt)
            .ThenByDescending(r => r.CustomerRestrictionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.CustomerRestrictionId,
                r.UserId,
                r.User.FullName,
                r.User.Email,
                r.Reason,
                r.Status,
                r.ExpiredOrderCount,
                r.StartedAt,
                r.ExpiresAt,
                r.LiftedAt,
                r.LiftedByUserId,
                LiftedByName = r.LiftedBy != null ? r.LiftedBy.FullName : null,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new CustomerRestrictionDto(
                r.CustomerRestrictionId,
                r.UserId,
                r.FullName,
                r.Email,
                r.Reason.ToString(),
                r.Status.ToString(),
                r.ExpiredOrderCount,
                r.StartedAt,
                r.ExpiresAt,
                r.LiftedAt,
                r.LiftedByUserId,
                r.LiftedByName
            ))
            .ToList();

        return OffsetPageResult<CustomerRestrictionDto>.Create(items, page, pageSize, total);
    }

    public async Task<CustomerRestrictionDto> LiftAsync(int id, CancellationToken cancellationToken)
    {
        var restriction =
            await db
                .CustomerRestrictions.AsNoTracking()
                .FirstOrDefaultAsync(r => r.CustomerRestrictionId == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_CustomerRestriction);

        var before = AuditSnapshots.CustomerRestrictionState(restriction);
        var now = DateTime.UtcNow;
        var actorId = actorAccessor.Current?.UserId;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var lifted = await db
            .CustomerRestrictions.Where(r =>
                r.CustomerRestrictionId == id
                && r.Status == CustomerRestrictionStatus.Active
                && r.ExpiresAt > now
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(r => r.Status, CustomerRestrictionStatus.Lifted)
                        .SetProperty(r => r.LiftedAt, (DateTime?)now)
                        .SetProperty(r => r.LiftedByUserId, actorId),
                cancellationToken
            );
        if (lifted == 0)
            throw new BusinessRuleException(Messages.Rule_CustomerRestrictionNotActive);

        restriction.Status = CustomerRestrictionStatus.Lifted;
        restriction.LiftedAt = now;
        restriction.LiftedByUserId = actorId;
        auditLogger.Record(
            AuditActions.LiftCustomerRestriction,
            AuditEntityTypes.CustomerRestriction,
            id.ToString(),
            before,
            AuditSnapshots.CustomerRestrictionState(restriction)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var row = await db
            .CustomerRestrictions.AsNoTracking()
            .Where(r => r.CustomerRestrictionId == id)
            .Select(r => new CustomerRestrictionDto(
                r.CustomerRestrictionId,
                r.UserId,
                r.User.FullName,
                r.User.Email,
                r.Reason.ToString(),
                r.Status.ToString(),
                r.ExpiredOrderCount,
                r.StartedAt,
                r.ExpiresAt,
                r.LiftedAt,
                r.LiftedByUserId,
                r.LiftedBy != null ? r.LiftedBy.FullName : null
            ))
            .FirstAsync(cancellationToken);
        return row;
    }

    private IQueryable<Order> ExpiredStreakQuery(int userId) =>
        db.Orders.AsNoTracking()
            .Where(o =>
                o.UserId == userId
                && o.Channel == SalesChannel.Online
                && o.Type == OrderType.Ticket
                && o.Status == OrderStatus.Expired
                && !db.Orders.Any(c =>
                    c.UserId == userId
                    && c.Type == OrderType.Ticket
                    && c.Status == OrderStatus.Confirmed
                    && c.CreatedAt >= o.CreatedAt
                )
            );

    private static ActiveCustomerRestriction ToActive(CustomerRestriction r) =>
        new(r.CustomerRestrictionId, r.Reason, r.ExpiredOrderCount, r.ExpiresAt);
}
