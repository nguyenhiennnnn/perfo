using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Support;

namespace Cinema.Application.Interfaces.Support;

public interface IExceptionalRefundService
{
    Task<OffsetPageResult<ExceptionalRefundProposalDto>> GetAsync(
        ExceptionalRefundProposalListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<ExceptionalRefundProposalDto> GetByIdAsync(int proposalId, CancellationToken cancellationToken);

    Task<ExceptionalRefundProposalDto> ProposeAsync(
        CreateExceptionalRefundProposalRequest request,
        CancellationToken cancellationToken
    );

    Task<ExceptionalRefundProposalDto> ApproveAsync(
        int proposalId,
        ApproveExceptionalRefundProposalRequest request,
        CancellationToken cancellationToken
    );

    Task<ExceptionalRefundProposalDto> RejectAsync(
        int proposalId,
        RejectExceptionalRefundProposalRequest request,
        CancellationToken cancellationToken
    );
}
