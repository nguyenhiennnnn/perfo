namespace Cinema.Application.DTOs.Dashboard;

public record CancellationTotalDto(
    int PaidOrders,
    int CancelledOrders,
    decimal? CancellationRatePercent,
    decimal RefundedAmount
);

public record CancellationGroupDto(
    string Key,
    int CancelledOrders,
    decimal RefundedAmount,
    decimal? SharePercent
);

public record CancellationMovieDto(
    int? MovieId,
    string? MovieTitle,
    int PaidOrders,
    int CancelledOrders,
    decimal? CancellationRatePercent,
    decimal RefundedAmount
);

public record CancellationAnalyticsDto(
    DateOnly From,
    DateOnly To,
    CancellationTotalDto Total,
    IReadOnlyList<CancellationGroupDto> ByReason,
    IReadOnlyList<CancellationGroupDto> ByCancelledBy,
    IReadOnlyList<CancellationGroupDto> ByTimeToShowtime,
    IReadOnlyList<CancellationMovieDto> ByMovie
);

public static class CancellationTimeBuckets
{
    public const string Under3Hours = "Under3Hours";
    public const string Hours3To24 = "Hours3To24";
    public const string Days1To3 = "Days1To3";
    public const string Over3Days = "Over3Days";
    public const string NoShowtime = "NoShowtime";

    public static readonly string[] All =
    [
        Under3Hours,
        Hours3To24,
        Days1To3,
        Over3Days,
        NoShowtime,
    ];
}
