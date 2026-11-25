using Cinema.Application.DTOs.Support;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Support;

public static class SupportRequestMapper
{
    public static SupportRequestDto ToDto(
        SupportRequest request,
        string orderCode,
        string? assigneeName,
        IEnumerable<SupportMessage> messages
    ) =>
        new(
            request.SupportRequestId,
            request.OrderId,
            orderCode,
            request.Type,
            request.Subject,
            request.Status,
            assigneeName,
            request.DueAt,
            request.CreatedAt,
            request.UpdatedAt,
            request.LastMessageAt,
            request.ResolvedAt,
            messages
                .OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.SupportMessageId)
                .Select(ToDto)
                .ToList()
        );

    private static SupportMessageDto ToDto(SupportMessage message) =>
        new(
            message.SupportMessageId,
            message.AuthorType,
            message.AuthorType == SupportMessageAuthorType.Staff
                ? message.AuthorUser?.FullName
                : null,
            message.Content,
            message.CreatedAt
        );
}
