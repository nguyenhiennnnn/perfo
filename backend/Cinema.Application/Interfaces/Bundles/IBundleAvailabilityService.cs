using Cinema.Application.DTOs.Bundles;

namespace Cinema.Application.Interfaces.Bundles;

public interface IBundleAvailabilityService
{
    Task<IReadOnlyList<ApplicableBundleDto>> GetForShowtimeAsync(
        int showtimeId,
        CancellationToken cancellationToken
    );
}
