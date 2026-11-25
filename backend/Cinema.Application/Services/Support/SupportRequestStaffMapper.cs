using Cinema.Application.DTOs.Support;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Support;

public static class SupportRequestStaffMapper
{
    public static StaffSupportRequestDto ToDto(SupportRequest request, DateTime now) =>
        new(
            request.SupportRequestId,
            request.OrderId,
            request.Order.OrderCode,
            request.UserId,
            request.User?.FullName,
            request.User?.Email,
            request.Type,
            request.Subject,
            request.Status,
            request.AssignedToUserId,
            request.AssignedToUser?.FullName,
            request.DueAt,
            IsOverdue(request.Status, request.DueAt, now),
            request.CreatedAt,
            request.UpdatedAt,
            request.LastMessageAt,
            request.ResolvedAt,
            request.ApologyPromotion is { } promotion
                ? new SupportApologyCodeDto(
                    promotion.PromotionId,
                    promotion.Code,
                    promotion.DiscountType,
                    promotion.DiscountValue,
                    promotion.MaxDiscountAmount,
                    promotion.EndsAt,
                    promotion.ConsumedCount > 0
                )
                : null,
            request
                .Messages.OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.SupportMessageId)
                .Select(m => new StaffSupportMessageDto(
                    m.SupportMessageId,
                    m.AuthorType,
                    m.AuthorUserId,
                    m.AuthorUser?.FullName,
                    m.Content,
                    m.CreatedAt
                ))
                .ToList()
        );

    public static bool IsOverdue(SupportRequestStatus status, DateTime dueAt, DateTime now) =>
        status != SupportRequestStatus.Resolved && dueAt < now;
}
