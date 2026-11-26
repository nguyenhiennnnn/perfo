using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Orders;

public class OrderQueryService(
    IAppDbContext db,
    IPaymentLedgerService paymentLedger,
    IOptions<BookingOptions> bookingOptions
) : IOrderQueryService
{
    private readonly BookingOptions _options = bookingOptions.Value;

    public async Task<CursorPageResult<OrderListItemDto>> GetMyOrdersAsync(
        int userId,
        OrderListFilter filter,
        string? cursor,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var query = db.Orders.AsNoTracking().Where(o => o.UserId == userId).ApplyFilter(filter);

        var cursorId = CursorCodec.Decode(cursor);
        if (cursorId is not null)
            query = query.Where(o => o.OrderId < cursorId);

        var rows = await query
            .OrderByDescending(o => o.OrderId)
            .Take(limit + 1)
            .SelectListRows()
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var items = rows.Take(limit).Select(r => r.ToDto()).ToList();

        return new CursorPageResult<OrderListItemDto>
        {
            Items = items,
            NextCursor = hasMore ? CursorCodec.Encode(items[^1].OrderId) : null,
            HasMore = hasMore,
        };
    }

    public Task<OrderDetailDto> GetMyOrderDetailAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    ) => GetDetailAsync(orderId, userId, cancellationToken);

    public async Task<OffsetPageResult<OrderListItemDto>> GetOrdersAdminAsync(
        OrderListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(1, page);
        pageSize =
            pageSize <= 0 ? _options.DefaultPageSize : Math.Min(pageSize, _options.MaxPageSize);

        var query = db.Orders.AsNoTracking().ApplyFilter(filter);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.OrderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .SelectListRows()
            .ToListAsync(cancellationToken);

        return OffsetPageResult<OrderListItemDto>.Create(
            rows.Select(r => r.ToDto()).ToList(),
            page,
            pageSize,
            total
        );
    }

    public Task<OrderDetailDto> GetOrderDetailAdminAsync(
        int orderId,
        CancellationToken cancellationToken
    ) => GetDetailAsync(orderId, null, cancellationToken);

    private async Task<OrderDetailDto> GetDetailAsync(
        int orderId,
        int? ownerUserId,
        CancellationToken cancellationToken
    )
    {
        var order =
            await db
                .Orders.AsNoTracking()
                .AsSplitQuery()
                .Include(o => o.CreatedByEmployee)
                    .ThenInclude(e => e!.User)
                .Include(o => o.PriceAdjustments)
                    .ThenInclude(a => a.Allocations)
                .Include(o => o.PaymentTransactions)
                .Include(o => o.Lines)
                    .ThenInclude(l => l.Combo)
                .Include(o => o.Lines)
                    .ThenInclude(l => l.TicketType)
                .Include(o => o.Lines)
                    .ThenInclude(l => l.ShowtimeSeat)
                        .ThenInclude(ss => ss!.Seat)
                .Include(o => o.Lines)
                    .ThenInclude(l => l.Showtime)
                        .ThenInclude(s => s!.Movie)
                .Include(o => o.Lines)
                    .ThenInclude(l => l.Showtime)
                        .ThenInclude(s => s!.Room)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        if (ownerUserId is not null && order.UserId != ownerUserId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);

        var summary = await paymentLedger.GetSummaryAsync(orderId, cancellationToken);
        var tierNames = await LoadTierNamesAsync(order.PriceAdjustments, cancellationToken);
        var promotions = await LoadPromotionsAsync(order.PriceAdjustments, cancellationToken);
        var bundleNames = await LoadBundleNamesAsync(order.PriceAdjustments, cancellationToken);

        var exchanges = await LoadExchangesAsync(orderId, cancellationToken);

        return OrderDetailMapper.ToDetail(
            order,
            summary,
            tierNames,
            promotions,
            bundleNames,
            ownerUserId is null,
            exchanges
        );
    }

    private async Task<List<OrderExchangeHistoryDto>> LoadExchangesAsync(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var rows = await db
            .OrderExchanges.AsNoTracking()
            .Where(e => e.OrderId == orderId || e.NewOrderLine.OrderId == orderId)
            .OrderBy(e => e.ExchangeNumber)
            .ThenBy(e => e.OrderExchangeId)
            .Select(e => new
            {
                e.OrderExchangeId,
                e.ExchangeNumber,
                e.OrderId,
                OriginalOrderCode = e.Order.OrderCode,
                NewOrderId = e.NewOrderLine.OrderId,
                NewOrderCode = e.NewOrderLine.Order.OrderCode,
                e.NewOrderLine.Order.Channel,
                e.ExchangedAt,
                e.OldOrderLineId,
                e.NewOrderLineId,
                e.OldShowtimeId,
                MovieTitle = e.OldShowtime.Movie.Title,
                OldRoomName = e.OldShowtime.Room.RoomName,
                OldStartTime = e.OldShowtime.StartTime,
                OldSeatRow = e.OldOrderLine.ShowtimeSeat!.Seat.SeatRow,
                OldSeatNumber = e.OldOrderLine.ShowtimeSeat!.Seat.SeatNumber,
                e.NewShowtimeId,
                NewRoomName = e.NewShowtime.Room.RoomName,
                NewStartTime = e.NewShowtime.StartTime,
                NewSeatRow = e.NewOrderLine.ShowtimeSeat!.Seat.SeatRow,
                NewSeatNumber = e.NewOrderLine.ShowtimeSeat!.Seat.SeatNumber,
                e.PriceDifference,
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(r => new OrderExchangeHistoryDto(
                r.OrderExchangeId,
                r.ExchangeNumber,
                r.OrderId,
                r.OriginalOrderCode,
                r.NewOrderId,
                r.NewOrderCode,
                r.Channel.ToDtoChannel(),
                r.ExchangedAt,
                r.OldOrderLineId,
                r.NewOrderLineId,
                r.OldShowtimeId,
                r.MovieTitle,
                r.OldRoomName,
                r.OldStartTime,
                r.OldSeatRow,
                r.OldSeatNumber,
                r.NewShowtimeId,
                r.NewRoomName,
                r.NewStartTime,
                r.NewSeatRow,
                r.NewSeatNumber,
                r.PriceDifference
            )),
        ];
    }

    private async Task<Dictionary<int, OrderPromotionInfo>> LoadPromotionsAsync(
        IEnumerable<OrderPriceAdjustment> adjustments,
        CancellationToken cancellationToken
    )
    {
        var promotionIds = adjustments
            .Where(a => a.ReferenceType == nameof(Promotion) && a.ReferenceId is not null)
            .Select(a => a.ReferenceId!.Value)
            .Distinct()
            .ToList();

        if (promotionIds.Count == 0)
            return [];

        return await db
            .Promotions.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => promotionIds.Contains(p.PromotionId))
            .Select(p => new
            {
                p.PromotionId,
                p.Code,
                p.Name,
                p.Type,
            })
            .ToDictionaryAsync(
                p => p.PromotionId,
                p => new OrderPromotionInfo(p.Code, p.Name, p.Type != PromotionType.AutoApply),
                cancellationToken
            );
    }

    private async Task<Dictionary<int, string>> LoadBundleNamesAsync(
        IEnumerable<OrderPriceAdjustment> adjustments,
        CancellationToken cancellationToken
    )
    {
        var bundleIds = adjustments
            .Where(a => a.ReferenceType == nameof(Bundle) && a.ReferenceId is not null)
            .Select(a => a.ReferenceId!.Value)
            .Distinct()
            .ToList();

        if (bundleIds.Count == 0)
            return [];

        return await db
            .Bundles.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => bundleIds.Contains(b.BundleId))
            .ToDictionaryAsync(b => b.BundleId, b => b.Name, cancellationToken);
    }

    private async Task<Dictionary<int, string>> LoadTierNamesAsync(
        IEnumerable<OrderPriceAdjustment> adjustments,
        CancellationToken cancellationToken
    )
    {
        var tierIds = adjustments
            .Where(a => a.ReferenceType == nameof(MembershipTier) && a.ReferenceId is not null)
            .Select(a => a.ReferenceId!.Value)
            .Distinct()
            .ToList();

        if (tierIds.Count == 0)
            return [];

        return await db
            .MembershipTiers.AsNoTracking()
            .Where(t => tierIds.Contains(t.TierId))
            .ToDictionaryAsync(t => t.TierId, t => t.TierName, cancellationToken);
    }
}
