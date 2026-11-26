using Cinema.Application.Caching;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Utils;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalQuoteService(IAppDbContext db, IAppCache cache) : IRoomRentalQuoteService
{
    public async Task<RoomRentalQuoteResultDto> CalculateAsync(
        RoomRentalQuoteRequest request,
        CancellationToken cancellationToken
    )
    {
        var room =
            await db
                .CinemaRooms.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoomId == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Room);
        if (!room.IsActive)
            throw new BusinessRuleException(Messages.Rule_RoomRentalQuoteRoomInactive);

        var startLocal =
            request.StartAt.Kind == DateTimeKind.Utc
                ? BusinessTime.ToLocal(request.StartAt)
                : request.StartAt;

        var rates = await cache.GetAsync(CacheKeys.RoomRentalRates, cancellationToken);
        var rate =
            RoomRentalRateMatcher.Find(
                rates,
                room.RoomId,
                TimeOnly.FromDateTime(startLocal),
                request.DurationMinutes
            ) ?? throw new BusinessRuleException(Messages.Rule_RoomRentalRateNotFound);

        var combos = await LoadCombosAsync(request.Combos, cancellationToken);

        var build = RoomRentalQuoteCalculator.Build(
            rate,
            room.RoomName,
            request.DurationMinutes,
            request.AttendeeCount,
            combos,
            request.Extras ?? [],
            request.DiscountAmount,
            request.DiscountPercent,
            request.DiscountReason
        );

        return new RoomRentalQuoteResultDto(
            room.RoomId,
            room.RoomName,
            rate.RoomRentalRateId,
            rate.Name,
            startLocal,
            request.DurationMinutes,
            request.AttendeeCount,
            build.Lines,
            build.Subtotal,
            build.DiscountTotal,
            build.Total
        );
    }

    private async Task<List<RoomRentalQuoteCombo>> LoadCombosAsync(
        List<RoomRentalQuoteComboItem>? items,
        CancellationToken cancellationToken
    )
    {
        if (items is not { Count: > 0 })
            return [];

        var ids = items.Select(i => i.ComboId).ToList();
        var combos = await db
            .Combos.AsNoTracking()
            .Where(c => ids.Contains(c.ComboId))
            .ToDictionaryAsync(c => c.ComboId, cancellationToken);

        var result = new List<RoomRentalQuoteCombo>(items.Count);
        foreach (var item in items)
        {
            if (!combos.TryGetValue(item.ComboId, out var combo))
                throw new NotFoundException(Messages.NotFound_Combo);
            if (!combo.IsActive)
                throw new BusinessRuleException(
                    string.Format(Messages.Rule_RoomRentalQuoteComboInactive, combo.Name)
                );
            result.Add(new RoomRentalQuoteCombo(combo.ComboId, combo.Name, combo.Price, item.Quantity));
        }

        return result;
    }
}
