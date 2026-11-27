using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Memberships;

public class MembershipSubscriptionQueryService(IAppDbContext db)
    : IMembershipSubscriptionQueryService
{
    public async Task<IReadOnlyList<MembershipSubscriptionDto>> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var rows = await db
            .MembershipSubscriptions.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ValidFrom)
            .Take(Pagination.MaxPageSize)
            .Select(s => new
            {
                s.MembershipSubscriptionId,
                s.MembershipPlanId,
                PlanName = s.MembershipPlan.Name,
                s.OrderId,
                s.ValidFrom,
                s.ValidTo,
                s.Status,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(s => new MembershipSubscriptionDto(
                s.MembershipSubscriptionId,
                s.MembershipPlanId,
                s.PlanName,
                s.OrderId,
                s.ValidFrom,
                s.ValidTo,
                s.Status == MembershipSubscriptionStatus.Active
                    && s.ValidFrom <= now
                    && s.ValidTo > now,
                s.Status.ToString()
            ))
            .ToList();
    }

    public async Task<OffsetPageResult<AdminMembershipSubscriptionDto>> GetAdminAsync(
        int page,
        int pageSize,
        MembershipSubscriptionListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);
        var now = DateTime.UtcNow;

        var query = db.MembershipSubscriptions.AsNoTracking().IgnoreQueryFilters();
        if (
            EnumConverters.TryParseEnum<MembershipSubscriptionStatus>(filter.Status, out var status)
            && Enum.IsDefined(status)
        )
            query = query.Where(s => s.Status == status);
        if (filter.PlanId is { } planId)
            query = query.Where(s => s.MembershipPlanId == planId);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(s =>
                    EF.Functions.ILike(s.User.FullName, pattern)
                    || EF.Functions.ILike(s.User.Email, pattern)
                    || EF.Functions.ILike(s.Order.OrderCode, pattern)
                    || (s.User.Phone != null && EF.Functions.ILike(s.User.Phone, pattern))
                )
        );

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.MembershipSubscriptionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.MembershipSubscriptionId,
                s.UserId,
                CustomerName = s.User.FullName,
                CustomerEmail = s.User.Email,
                CustomerPhone = s.User.Phone,
                s.MembershipPlanId,
                PlanName = s.MembershipPlan.Name,
                s.OrderId,
                s.Order.OrderCode,
                PlanPrice = s.MembershipPlan.Price,
                s.ValidFrom,
                s.ValidTo,
                s.Status,
                s.RefundedAt,
                s.RefundReason,
                s.StoppedRevenueAmount,
                s.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return OffsetPageResult<AdminMembershipSubscriptionDto>.Create(
            rows.Select(s => new AdminMembershipSubscriptionDto(
                    s.MembershipSubscriptionId,
                    s.UserId,
                    s.CustomerName,
                    s.CustomerEmail,
                    s.CustomerPhone,
                    s.MembershipPlanId,
                    s.PlanName,
                    s.OrderId,
                    s.OrderCode,
                    s.PlanPrice,
                    s.ValidFrom,
                    s.ValidTo,
                    s.Status == MembershipSubscriptionStatus.Active
                        && s.ValidFrom <= now
                        && s.ValidTo > now,
                    s.Status.ToString(),
                    s.RefundedAt,
                    s.RefundReason?.ToString(),
                    s.StoppedRevenueAmount,
                    s.CreatedAt
                ))
                .ToList(),
            page,
            pageSize,
            total
        );
    }
}
