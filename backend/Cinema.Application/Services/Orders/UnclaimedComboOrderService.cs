using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class UnclaimedComboOrderService(IAppDbContext db) : IUnclaimedComboOrderService
{
    public async Task<OffsetPageResult<UnclaimedComboOrderDto>> GetAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var now = DateTime.UtcNow;
        var cutoff = ComboPickupRules.UnclaimedCutoffUtc(now);
        var query = db
            .Orders.AsNoTracking()
            .Where(o =>
                o.Type == OrderType.Combo
                && o.Status == OrderStatus.Confirmed
                && o.CreatedAt < cutoff
                && o.Lines.Any(l =>
                    l.LineType == OrderLineType.Combo
                    && l.Quantity - l.RefundedQuantity - l.QuantityFulfilled > 0
                )
            )
            .ApplySearch(
                search,
                (q, pattern) =>
                    q.Where(o =>
                        EF.Functions.ILike(o.OrderCode, pattern)
                        || (o.CustomerName != null && EF.Functions.ILike(o.CustomerName, pattern))
                        || (o.CustomerPhone != null && EF.Functions.ILike(o.CustomerPhone, pattern))
                    )
            );

        var total = await query.CountAsync(cancellationToken);
        var orders = await query
            .OrderBy(o => o.CreatedAt)
            .ThenBy(o => o.OrderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.OrderId,
                o.OrderCode,
                o.CustomerName,
                o.CustomerPhone,
                o.CreatedAt,
                o.FinalAmount,
            })
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.OrderId).ToList();
        var lines = await db
            .OrderLines.AsNoTracking()
            .Where(l => orderIds.Contains(l.OrderId) && l.LineType == OrderLineType.Combo)
            .OrderBy(l => l.OrderLineId)
            .Select(l => new
            {
                l.OrderId,
                ComboName = l.Combo!.Name,
                l.Quantity,
                l.RefundedQuantity,
                l.QuantityFulfilled,
            })
            .ToListAsync(cancellationToken);
        var linesByOrder = lines.ToLookup(l => l.OrderId);

        var today = BusinessTime.Today(now);
        var items = orders
            .Select(o =>
            {
                var orderLines = linesByOrder[o.OrderId].ToList();
                var saleDate = BusinessTime.LocalDate(o.CreatedAt);
                return new UnclaimedComboOrderDto(
                    o.OrderId,
                    o.OrderCode,
                    o.CustomerName,
                    o.CustomerPhone,
                    o.CreatedAt,
                    saleDate,
                    today.DayNumber - saleDate.DayNumber,
                    o.FinalAmount,
                    orderLines.Sum(l => l.Quantity - l.RefundedQuantity),
                    orderLines.Sum(l => l.QuantityFulfilled),
                    orderLines.Sum(l =>
                        ComboPickupRules.Remaining(
                            l.Quantity,
                            l.RefundedQuantity,
                            l.QuantityFulfilled
                        )
                    ),
                    [
                        .. orderLines.Select(l => new UnclaimedComboLineDto(
                            l.ComboName,
                            l.Quantity - l.RefundedQuantity,
                            ComboPickupRules.Remaining(
                                l.Quantity,
                                l.RefundedQuantity,
                                l.QuantityFulfilled
                            )
                        )),
                    ]
                );
            })
            .ToList();

        return OffsetPageResult<UnclaimedComboOrderDto>.Create(items, page, pageSize, total);
    }
}
