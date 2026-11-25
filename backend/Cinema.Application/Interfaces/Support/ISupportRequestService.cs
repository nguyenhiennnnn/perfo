using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Support;
using Cinema.Domain.Enums;

namespace Cinema.Application.Interfaces.Support;

public interface ISupportRequestService
{
    Task<OffsetPageResult<SupportRequestListItemDto>> GetForCustomerAsync(
        int userId,
        SupportRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<SupportRequestDto> GetForCustomerAsync(
        int userId,
        int requestId,
        CancellationToken cancellationToken
    );

    Task<SupportRequestDto> CreateForCustomerAsync(
        int userId,
        CreateSupportRequestRequest request,
        CancellationToken cancellationToken
    );

    Task<SupportRequestDto> ReplyForCustomerAsync(
        int userId,
        int requestId,
        ReplySupportRequestRequest request,
        CancellationToken cancellationToken
    );
}
