using Cinema.Application.Caching;

namespace Cinema.Application.Services.RoomRentals;

public static class RoomRentalRateMatcher
{
    public static RoomRentalRateSnapshot? Find(
        IReadOnlyList<RoomRentalRateSnapshot> rates,
        int roomId,
        TimeOnly startTime,
        int durationMinutes
    ) =>
        rates
            .Where(r =>
                r.RoomId == roomId
                && startTime >= r.StartTime
                && startTime < r.EndTime
                && durationMinutes >= r.MinDurationMinutes
                && durationMinutes <= r.MaxDurationMinutes
            )
            .OrderBy(r => r.RoomRentalRateId)
            .FirstOrDefault();
}
