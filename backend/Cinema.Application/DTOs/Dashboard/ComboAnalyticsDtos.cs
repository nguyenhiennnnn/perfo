using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Dashboard;

public record ComboRevenueItemDto(
    int ComboId,
    string? ComboName,
    int QuantitySold,
    int QuantityFulfilled,
    decimal? FulfilledPercent,
    decimal NetRevenue
);

public record ComboRevenueChannelDto(
    SalesChannel Channel,
    int QuantitySold,
    int QuantityFulfilled,
    decimal? FulfilledPercent,
    decimal NetRevenue
);

public record ComboRevenueTotalDto(
    int QuantitySold,
    int QuantityFulfilled,
    decimal? FulfilledPercent,
    decimal NetRevenue,
    int ConfirmedOrders,
    int OrdersWithCombo,
    decimal? ComboOrderRatePercent,
    decimal? AverageComboValuePerOrder
);

public record ComboRevenueDto(
    DateOnly From,
    DateOnly To,
    ComboRevenueTotalDto Total,
    IReadOnlyList<ComboRevenueItemDto> ByCombo,
    IReadOnlyList<ComboRevenueChannelDto> ByChannel
);
