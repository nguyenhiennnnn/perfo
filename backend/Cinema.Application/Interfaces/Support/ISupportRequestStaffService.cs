using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Support;

namespace Cinema.Application.Interfaces.Support;

public interface ISupportRequestStaffService
{
    Task<OffsetPageResult<StaffSupportRequestListItemDto>> GetAsync(
        SupportRequestStaffListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<StaffSupportRequestDto> GetByIdAsync(int requestId, CancellationToken cancellationToken);

    Task<StaffSupportRequestDto> AssignAsync(
        int requestId,
        AssignSupportRequestRequest request,
        CancellationToken cancellationToken
    );

    Task<StaffSupportRequestDto> ChangeStatusAsync(
        int requestId,
        ChangeSupportRequestStatusRequest request,
        CancellationToken cancellationToken
    );

    Task<StaffSupportRequestDto> ReplyAsync(
        int requestId,
        StaffReplySupportRequestRequest request,
        CancellationToken cancellationToken
    );

    Task<StaffSupportRequestDto> IssueApologyCodeAsync(
        int requestId,
        IssueSupportApologyCodeRequest request,
        CancellationToken cancellationToken
    );

    Task<StaffSupportRequestDto> ResendTicketEmailAsync(
        int requestId,
        CancellationToken cancellationToken
    );
}
