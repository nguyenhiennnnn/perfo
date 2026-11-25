using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Recommendations;

internal sealed class ShowtimeHabit(TasteLookup taste, decimal? typicalPrice)
{
    public static readonly ShowtimeHabit Empty = new(TasteLookup.Empty, null);

    public static ShowtimeHabit From(IEnumerable<CustomerTasteSignal> signals, decimal? typicalPrice) =>
        new(new TasteLookup(signals), typicalPrice);

    public decimal Fit(DateTime startUtc, int? screenTypeId, decimal? fromPrice)
    {
        var slot = TimeSlots.All.ToList().IndexOf(TimeSlots.Of(startUtc));
        var slotFit = taste.Normalized(TasteSignalKind.TimeSlot, slot);
        var screenFit = screenTypeId is { } id ? taste.Normalized(TasteSignalKind.ScreenType, id) : 0m;
        var priceFit =
            typicalPrice is > 0m && fromPrice is { } price
                ? 1m - Math.Min(1m, Math.Abs(price - typicalPrice.Value) / typicalPrice.Value)
                : 0m;

        return RecommendationWeights.SlotShare * slotFit
            + RecommendationWeights.ScreenShare * screenFit
            + RecommendationWeights.PriceShare * priceFit;
    }
}
