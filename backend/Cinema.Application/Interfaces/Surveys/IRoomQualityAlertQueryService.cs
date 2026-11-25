using Cinema.Application.DTOs.Surveys;

namespace Cinema.Application.Interfaces.Surveys;

public interface IRoomQualityAlertQueryService
{
    Task<IReadOnlyList<RoomQualityAlertDto>> GetAsync(
        bool activeOnly,
        CancellationToken cancellationToken
    );
}
