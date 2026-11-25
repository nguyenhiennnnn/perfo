using Cinema.Domain.Enums;

namespace Cinema.Application.DTOs.Support;

public record CreateSupportRequestRequest(
    int OrderId,
    SupportRequestType Type,
    string Subject,
    string Content
);

public record ReplySupportRequestRequest(string Content);

public record SupportRequestListItemDto(
    int SupportRequestId,
    int OrderId,
    string OrderCode,
    SupportRequestType Type,
    string Subject,
    SupportRequestStatus Status,
    DateTime DueAt,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    DateTime? ResolvedAt
);

public record SupportMessageDto(
    int SupportMessageId,
    SupportMessageAuthorType AuthorType,
    string? AuthorName,
    string Content,
    DateTime CreatedAt
);

public record SupportRequestDto(
    int SupportRequestId,
    int OrderId,
    string OrderCode,
    SupportRequestType Type,
    string Subject,
    SupportRequestStatus Status,
    string? AssigneeName,
    DateTime DueAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime LastMessageAt,
    DateTime? ResolvedAt,
    IReadOnlyList<SupportMessageDto> Messages
);
