using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.DTOs.RoomRentals;

namespace Cinema.Application.Interfaces.RoomRentals;

public interface IRoomRentalRateService
{
    Task<OffsetPageResult<RoomRentalRateDto>> GetAdminAsync(
        int page,
        int pageSize,
        RoomRentalRateListFilter filter,
        CancellationToken cancellationToken
    );

    Task<RoomRentalRateDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<RoomRentalRateDto> CreateAsync(
        UpsertRoomRentalRateRequest request,
        CancellationToken cancellationToken
    );

    Task<RoomRentalRateDto> UpdateAsync(
        int id,
        UpsertRoomRentalRateRequest request,
        CancellationToken cancellationToken
    );

    Task<DeletePricingEntityResponse> DeleteAsync(int id, CancellationToken cancellationToken);
}
