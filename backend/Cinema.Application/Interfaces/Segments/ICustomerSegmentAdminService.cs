using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Segments;
using Cinema.Application.Services.Segments;

namespace Cinema.Application.Interfaces.Segments;

public interface ICustomerSegmentAdminService
{
    Task<OffsetPageResult<CustomerSegmentDto>> GetAsync(
        int page,
        int pageSize,
        CustomerSegmentListFilter filter,
        CancellationToken cancellationToken
    );

    Task<CustomerSegmentDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<CustomerSegmentDto> CreateAsync(
        UpsertCustomerSegmentRequest request,
        CancellationToken cancellationToken
    );

    Task<CustomerSegmentDto> UpdateAsync(
        int id,
        UpsertCustomerSegmentRequest request,
        CancellationToken cancellationToken
    );

    Task<DeleteCustomerSegmentResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}

public interface ICustomerSegmentQueryService
{
    Task<(int CustomerCount, int EligibleCount)> CountAsync(
        SegmentConditionNode conditions,
        CancellationToken cancellationToken
    );

    Task<OffsetPageResult<SegmentCustomerDto>> GetCustomersAsync(
        int segmentId,
        int page,
        int pageSize,
        SegmentCustomerListFilter filter,
        CancellationToken cancellationToken
    );
}
