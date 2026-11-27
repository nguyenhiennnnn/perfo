using Cinema.Application.Services.Segments;

namespace Cinema.Application.DTOs.Segments;

public record UpsertCustomerSegmentRequest(
    string Name,
    string? Description,
    SegmentConditionNode Conditions,
    bool IsActive = true
);

public record CustomerSegmentDto(
    int SegmentId,
    string Name,
    string? Description,
    SegmentConditionNode Conditions,
    bool IsActive,
    int CustomerCount,
    int EligibleCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CustomerSegmentListFilter(string? Search, bool? IsActive);

public record DeleteCustomerSegmentResponse(int Id, bool Deleted);

public record SegmentCustomerListFilter(
    string? Search,
    bool EligibleOnly,
    bool RevealPersonalData
);

public record SegmentCustomerDto(
    int UserId,
    string FullName,
    string? Email,
    string? Phone,
    bool PersonalDataMasked,
    bool CanReceiveMarketing,
    string ActivityStage,
    DateTime? LastOrderAt,
    int? DaysSinceLastOrder,
    int LookbackOrderCount,
    decimal LookbackSpent,
    decimal TotalSpent,
    decimal EstimatedLifetimeValue,
    int ExpiringPoints,
    bool IsNearTierUpgrade
);
