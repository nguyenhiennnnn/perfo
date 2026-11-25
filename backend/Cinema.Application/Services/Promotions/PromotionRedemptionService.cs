using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Promotions;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Promotions;

public class PromotionRedemptionService(IAppDbContext db) : IPromotionRedemptionService
{
    public async Task<OffsetPageResult<PromotionRedemptionDto>> GetAsync(
        PromotionRedemptionListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        if (!await db.Promotions.AnyAsync(p => p.PromotionId == filter.PromotionId, cancellationToken))
            throw new NotFoundException(Messages.NotFound_Promotion);

        var query = db.PromotionRedemptions.AsNoTracking().ApplyFilter(filter);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(r => r.ReservedAt)
            .ThenByDescending(r => r.PromotionRedemptionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RedemptionRow(
                r.PromotionRedemptionId,
                r.OrderId,
                r.Order.OrderCode,
                r.Order.Channel,
                r.UserId,
                r.Order.CustomerName ?? (r.User == null ? null : r.User.FullName),
                r.Status,
                r.DiscountAmount,
                r.Order.FinalAmount,
                r.ReservedAt,
                r.RedeemedAt,
                r.ReleasedAt
            ))
            .ToListAsync(cancellationToken);

        return OffsetPageResult<PromotionRedemptionDto>.Create(
            rows.Select(ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    private static PromotionRedemptionDto ToDto(RedemptionRow r) =>
        new(
            r.PromotionRedemptionId,
            r.OrderId,
            r.OrderCode,
            r.Channel.ToString(),
            r.UserId,
            r.CustomerName,
            r.Status.ToString(),
            r.DiscountAmount,
            r.OrderFinalAmount,
            r.ReservedAt,
            r.RedeemedAt,
            r.ReleasedAt
        );

    private sealed record RedemptionRow(
        long PromotionRedemptionId,
        int OrderId,
        string OrderCode,
        SalesChannel Channel,
        int? UserId,
        string? CustomerName,
        PromotionRedemptionStatus Status,
        decimal DiscountAmount,
        decimal OrderFinalAmount,
        DateTime ReservedAt,
        DateTime? RedeemedAt,
        DateTime? ReleasedAt
    );
}
