using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Rooms;

namespace Cinema.Application.Interfaces.Rooms;

public interface IRoomClosureService
{
    Task<OffsetPageResult<RoomClosureDto>> GetAdminAsync(
        RoomClosureListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
    Task<RoomClosureDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<RoomClosureDto> CreateAsync(
        CreateRoomClosureRequest request,
        CancellationToken cancellationToken
    );
    Task<DeleteRoomClosureResponse> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<AffectedShowtimesResponse> GetAffectedShowtimesAsync(
        int roomId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken
    );
    Task<AffectedShowtimesResponse> GetClosureAffectedShowtimesAsync(
        int id,
        CancellationToken cancellationToken
    );
}
