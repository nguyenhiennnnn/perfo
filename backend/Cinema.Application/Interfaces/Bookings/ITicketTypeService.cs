using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pricing;

namespace Cinema.Application.Interfaces.Bookings;

public interface ITicketTypeService
{
    Task<OffsetPageResult<TicketTypeDto>> GetAdminAsync(
        int page,
        int pageSize,
        TicketTypeListFilter filter,
        CancellationToken cancellationToken
    );

    Task<TicketTypeDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<TicketTypeDto> CreateAsync(
        UpsertTicketTypeRequest request,
        CancellationToken cancellationToken
    );

    Task<TicketTypeDto> UpdateAsync(
        int id,
        UpsertTicketTypeRequest request,
        CancellationToken cancellationToken
    );

    Task<DeletePricingEntityResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}
