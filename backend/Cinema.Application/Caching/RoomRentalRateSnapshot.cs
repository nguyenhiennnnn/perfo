using Cinema.Domain.Entities;

namespace Cinema.Application.Caching;

public sealed record RoomRentalRateSnapshot(
    int RoomRentalRateId,
    string Name,
    int RoomId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MinDurationMinutes,
    int MaxDurationMinutes,
    decimal BasePrice,
    decimal PerPersonFee,
    int IncludedAttendees
)
{
    public static RoomRentalRateSnapshot From(RoomRentalRate r) =>
        new(
            r.RoomRentalRateId,
            r.Name,
            r.RoomId,
            r.StartTime,
            r.EndTime,
            r.MinDurationMinutes,
            r.MaxDurationMinutes,
            r.BasePrice,
            r.PerPersonFee,
            r.IncludedAttendees
        );
}
