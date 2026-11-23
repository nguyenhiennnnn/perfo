using Cinema.Application.DTOs.Common;

namespace Cinema.Application.DTOs.Promotions;

public record PromotionReportFilter(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Search,
    string? Type,
    bool? IsActive
);

public record PromotionReportSummaryDto(
    int PromotionCount,
    int RedeemedCount,
    int OrderCount,
    decimal TotalDiscount,
    decimal OrderRevenue
);

public record PromotionReportListItemDto(
    int PromotionId,
    string Code,
    string Name,
    string Type,
    bool IsActive,
    DateTime? StartsAt,
    DateTime? EndsAt,
    int RedeemedCount,
    decimal TotalDiscount,
    decimal OrderRevenue,
    int? TotalUsageLimit,
    int ConsumedCount,
    decimal? UsageRatio,
    decimal? BudgetAmount,
    decimal ConsumedBudget,
    decimal? BudgetRatio
);

public record PromotionReportListDto(
    PromotionReportSummaryDto Summary,
    OffsetPageResult<PromotionReportListItemDto> Items
);

public record PromotionReportUsageDto(
    int RedeemedCount,
    int ActiveHoldCount,
    int ReleasedCount,
    int OrderCount,
    int CustomerCount,
    decimal TotalDiscount,
    decimal OrderRevenue,
    decimal AverageDiscountPerOrder
);

public record PromotionReportLimitsDto(
    int? TotalUsageLimit,
    int ConsumedCount,
    int? RemainingUsage,
    decimal? UsageRatio,
    decimal? BudgetAmount,
    decimal ConsumedBudget,
    decimal? RemainingBudget,
    decimal? BudgetRatio
);

public record PromotionReportBreakdownItemDto(
    string Key,
    int RedeemedCount,
    decimal TotalDiscount,
    decimal OrderRevenue
);

public record PromotionReportDto(
    PromotionDto Promotion,
    PromotionReportUsageDto Usage,
    PromotionReportLimitsDto Limits,
    List<PromotionReportBreakdownItemDto> ByChannel,
    List<PromotionReportBreakdownItemDto> ByTimeSlot
);
