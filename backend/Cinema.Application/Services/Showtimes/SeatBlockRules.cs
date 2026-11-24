using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Showtimes;

public static class SeatBlockLimits
{
    public const int MaxSeatsPerRequest = 200;
    public const int ReleaseBatchSize = 500;
}

public static class SeatBlockRules
{
    public static readonly IReadOnlyCollection<SeatBlockType> ManualTypes =
    [
        SeatBlockType.Maintenance,
        SeatBlockType.Broken,
    ];

    public static bool TryParseManualType(string? value, out SeatBlockType type) =>
        Enum.TryParse(value, true, out type) && ManualTypes.Contains(type);
}
