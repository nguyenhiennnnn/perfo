using Cinema.Application.DTOs.Surveys;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Surveys;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Surveys;

public class RoomQualityAlertQueryService(IAppDbContext db) : IRoomQualityAlertQueryService
{
    private const int MaxResolvedRows = 200;

    public async Task<IReadOnlyList<RoomQualityAlertDto>> GetAsync(
        bool activeOnly,
        CancellationToken cancellationToken
    )
    {
        var query = db.RoomQualityAlerts.AsNoTracking();
        if (activeOnly)
            query = query.Where(a => a.ResolvedAt == null);

        return await query
            .OrderBy(a => a.ResolvedAt != null)
            .ThenByDescending(a => a.DetectedAt)
            .ThenByDescending(a => a.RoomQualityAlertId)
            .Take(activeOnly ? int.MaxValue : MaxResolvedRows)
            .Select(a => new RoomQualityAlertDto(
                a.RoomQualityAlertId,
                a.RoomId,
                a.Room.RoomName,
                a.FromDate,
                a.ToDate,
                a.ConsecutiveDays,
                a.AverageScore,
                a.Threshold,
                a.DetectedAt,
                a.NotifiedAt,
                a.ResolvedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
