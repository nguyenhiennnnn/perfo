using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Support;

public record SupportRequestStaffListFilter(
    SupportRequestStatus? Status,
    int? AssigneeId,
    bool? Unassigned,
    string? Search
);

public record AssignSupportRequestRequest(int? AssigneeId);

public record ChangeSupportRequestStatusRequest(SupportRequestStatus Status);

public record StaffReplySupportRequestRequest(string Content, SupportRequestStatus? Status);

public record IssueSupportApologyCodeRequest(
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    int? ValidDays
);

public record StaffSupportRequestListItemDto(
    int SupportRequestId,
    int OrderId,
    string OrderCode,
    int? CustomerId,
    string? CustomerName,
    SupportRequestType Type,
    string Subject,
    SupportRequestStatus Status,
    int? AssigneeId,
    string? AssigneeName,
    DateTime DueAt,
    bool IsOverdue,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    DateTime? ResolvedAt
);

public record StaffSupportMessageDto(
    int SupportMessageId,
    SupportMessageAuthorType AuthorType,
    int? AuthorUserId,
    string? AuthorName,
    string Content,
    DateTime CreatedAt
);

public record SupportApologyCodeDto(
    int PromotionId,
    string Code,
    PromotionDiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    DateTime? EndsAt,
    bool IsUsed
);

public record StaffSupportRequestDto(
    int SupportRequestId,
    int OrderId,
    string OrderCode,
    int? CustomerId,
    string? CustomerName,
    string? CustomerEmail,
    SupportRequestType Type,
    string Subject,
    SupportRequestStatus Status,
    int? AssigneeId,
    string? AssigneeName,
    DateTime DueAt,
    bool IsOverdue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime LastMessageAt,
    DateTime? ResolvedAt,
    SupportApologyCodeDto? ApologyCode,
    IReadOnlyList<StaffSupportMessageDto> Messages
);
