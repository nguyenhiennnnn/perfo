using Cinema.Application.Caching;
using Cinema.Application.DTOs.Bundles;
using Cinema.Application.Services.Pricing;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Bundles;

public static class BundleMapper
{
    public static BundleDto ToDto(Bundle b) =>
        new(
            b.BundleId,
            b.Name,
            b.Description,
            b.Price,
            [
                .. b
                    .Items.OrderBy(i => i.TicketTypeId == null)
                    .ThenBy(i => i.BundleItemId)
                    .Select(i => new BundleItemDto(
                        i.BundleItemId,
                        i.TicketTypeId,
                        i.TicketType?.Name,
                        i.ComboId,
                        i.Combo?.Name,
                        i.Quantity
                    )),
            ],
            b.ValidFrom,
            b.ValidTo,
            [.. b.MovieIds],
            [.. b.ShowtimeIds],
            [.. b.DaysOfWeek],
            [.. b.SeatTypeIds],
            PricingMapper.FormatTime(b.AllowedFromTime),
            PricingMapper.FormatTime(b.AllowedToTime),
            b.DailyLimit,
            b.ExcludesPromotions,
            b.IsActive,
            b.CreatedAt,
            b.UpdatedAt
        );

    public static List<BundleItemSnapshot> NormalizeItems(
        IEnumerable<UpsertBundleItemRequest>? items
    ) =>
        [
            .. (items ?? [])
                .GroupBy(i => (i.TicketTypeId, i.ComboId))
                .Select(g => new BundleItemSnapshot(
                    g.Key.TicketTypeId,
                    g.Key.ComboId,
                    g.Sum(i => i.Quantity)
                )),
        ];

    public static BundleRetailInput ToRetailInput(UpsertBundleRequest request) =>
        new(
            NormalizeItems(request.Items),
            PricingMapper.NormalizeIds(request.MovieIds),
            PricingMapper.NormalizeIds(request.ShowtimeIds),
            PricingMapper.NormalizeIds(request.DaysOfWeek),
            PricingMapper.NormalizeIds(request.SeatTypeIds),
            DateTimeUtils.ParseTimeOnly(request.AllowedFromTime),
            DateTimeUtils.ParseTimeOnly(request.AllowedToTime)
        );

    public static void Apply(Bundle entity, string name, UpsertBundleRequest request)
    {
        entity.Name = name;
        entity.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        entity.Price = request.Price;
        entity.ValidFrom = request.ValidFrom is { } validFrom ? BusinessTime.ToUtc(validFrom) : null;
        entity.ValidTo = request.ValidTo is { } validTo ? BusinessTime.ToUtc(validTo) : null;
        entity.MovieIds = PricingMapper.NormalizeIds(request.MovieIds);
        entity.ShowtimeIds = PricingMapper.NormalizeIds(request.ShowtimeIds);
        entity.DaysOfWeek = PricingMapper.NormalizeIds(request.DaysOfWeek);
        entity.SeatTypeIds = PricingMapper.NormalizeIds(request.SeatTypeIds);
        entity.AllowedFromTime = DateTimeUtils.ParseTimeOnly(request.AllowedFromTime);
        entity.AllowedToTime = DateTimeUtils.ParseTimeOnly(request.AllowedToTime);
        entity.DailyLimit = request.DailyLimit;
        entity.ExcludesPromotions = request.ExcludesPromotions;
        entity.IsActive = request.IsActive;
        ApplyItems(entity, NormalizeItems(request.Items));
    }

    private static void ApplyItems(Bundle entity, IReadOnlyList<BundleItemSnapshot> items)
    {
        foreach (
            var stale in entity
                .Items.Where(existing =>
                    !items.Any(i =>
                        i.TicketTypeId == existing.TicketTypeId && i.ComboId == existing.ComboId
                    )
                )
                .ToList()
        )
            entity.Items.Remove(stale);

        foreach (var item in items)
        {
            var existing = entity.Items.FirstOrDefault(i =>
                i.TicketTypeId == item.TicketTypeId && i.ComboId == item.ComboId
            );
            if (existing is null)
                entity.Items.Add(
                    new BundleItem
                    {
                        TicketTypeId = item.TicketTypeId,
                        ComboId = item.ComboId,
                        Quantity = item.Quantity,
                    }
                );
            else
                existing.Quantity = item.Quantity;
        }
    }
}
