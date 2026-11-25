using Cinema.Application.Caching;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Inventory;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Orders;

public class OrderComboSuggestionService(
    IAppDbContext db,
    IComboAvailabilityService availability,
    IAppCache cache
) : IOrderComboSuggestionService
{
    private const int MaxSuggestions = 3;

    private sealed record Candidate(
        int ComboId,
        string Name,
        string? Description,
        string? ImageUrl,
        decimal Price,
        int? SuggestedTicketCount
    );

    public async Task<OrderComboSuggestionsDto> GetAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var status =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId && o.UserId == userId)
                .Select(o => (OrderStatus?)o.Status)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var discountPercent = settings.PreorderComboDiscountPercent;
        var none = new OrderComboSuggestionsDto(orderId, false, 0, discountPercent, null, []);
        if (status != OrderStatus.Confirmed)
            return none;

        var tickets = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.OrderId == orderId
                && l.LineType == OrderLineType.Ticket
                && l.RefundedQuantity < l.Quantity
                && l.ShowtimeId != null
            )
            .ExcludingExchangedOut(db)
            .Select(l => new { l.Quantity, l.RefundedQuantity, l.Showtime!.StartTime })
            .ToListAsync(cancellationToken);
        if (tickets.Count == 0)
            return none;

        var validUntil = tickets.Min(t => t.StartTime);
        if (validUntil <= DateTime.UtcNow)
            return none;

        var ticketCount = tickets.Sum(t => t.Quantity - t.RefundedQuantity);
        var candidates = await db
            .Combos.AsNoTracking()
            .Where(c => c.IsActive)
            .InStock(db)
            .Select(c => new Candidate(
                c.ComboId,
                c.Name,
                c.Description,
                c.ImageUrl,
                c.Price,
                c.SuggestedTicketCount
            ))
            .ToListAsync(cancellationToken);

        var purchased = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.LineType == OrderLineType.Combo
                && l.ComboId != null
                && l.Order.UserId == userId
                && l.Order.Status == OrderStatus.Confirmed
            )
            .GroupBy(l => l.ComboId!.Value)
            .Select(g => new { ComboId = g.Key, Quantity = g.Sum(l => l.Quantity - l.RefundedQuantity) })
            .Where(x => x.Quantity > 0)
            .ToDictionaryAsync(x => x.ComboId, x => x.Quantity, cancellationToken);

        var items = new List<OrderComboSuggestionDto>();

        var byTickets = candidates
            .Where(c => c.SuggestedTicketCount is > 0)
            .Select(c =>
            {
                var size = c.SuggestedTicketCount!.Value;
                var quantity = (int)Math.Ceiling(ticketCount / (double)size);
                return (Combo: c, Quantity: quantity, Waste: quantity * size - ticketCount);
            })
            .OrderBy(x => x.Waste)
            .ThenBy(x => x.Quantity)
            .ThenBy(x => x.Combo.Price)
            .ToList();

        foreach (var match in byTickets)
        {
            var quantity = await FitQuantityAsync(match.Combo.ComboId, match.Quantity, cancellationToken);
            if (quantity == 0)
                continue;
            var size = match.Combo.SuggestedTicketCount!.Value;
            var label =
                quantity == 1
                    ? string.Format(Messages.Combo_SuggestionFitPeople, size)
                    : string.Format(Messages.Combo_SuggestionFitPeopleQuantity, quantity * size, quantity);
            items.Add(ToDto(match.Combo, quantity, label, true, purchased.ContainsKey(match.Combo.ComboId), discountPercent));
            break;
        }

        foreach (var combo in candidates
            .Where(c => purchased.ContainsKey(c.ComboId) && items.All(i => i.ComboId != c.ComboId))
            .OrderByDescending(c => purchased[c.ComboId])
            .ThenBy(c => c.Price))
        {
            if (items.Count >= MaxSuggestions)
                break;
            var wanted = combo.SuggestedTicketCount is > 0
                ? (int)Math.Ceiling(ticketCount / (double)combo.SuggestedTicketCount.Value)
                : 1;
            var quantity = await FitQuantityAsync(combo.ComboId, wanted, cancellationToken);
            if (quantity == 0)
                continue;
            items.Add(ToDto(combo, quantity, Messages.Combo_SuggestionPurchasedBefore, false, true, discountPercent));
        }

        return new OrderComboSuggestionsDto(orderId, true, ticketCount, discountPercent, validUntil, items);
    }

    private async Task<int> FitQuantityAsync(
        int comboId,
        int wanted,
        CancellationToken cancellationToken
    )
    {
        for (var quantity = wanted; quantity >= 1; quantity--)
        {
            try
            {
                await availability.EnsureAvailableAsync([(comboId, quantity)], cancellationToken);
                return quantity;
            }
            catch (ConflictAppException) { }
        }

        return 0;
    }

    private static OrderComboSuggestionDto ToDto(
        Candidate combo,
        int quantity,
        string label,
        bool matchesTicketCount,
        bool purchasedBefore,
        int discountPercent
    ) =>
        new(
            combo.ComboId,
            combo.Name,
            combo.Description,
            combo.ImageUrl,
            quantity,
            combo.Price,
            discountPercent > 0
                ? Math.Round(combo.Price * (100 - discountPercent) / 100m, 0, MidpointRounding.AwayFromZero)
                : combo.Price,
            label,
            matchesTicketCount,
            purchasedBefore
        );
}
