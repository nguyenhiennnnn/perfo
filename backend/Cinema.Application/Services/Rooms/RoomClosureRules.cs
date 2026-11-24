using Cinema.Application.Interfaces;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Rooms;

public static class RoomClosureLimits
{
    public const int MaxSpanDays = 366;
}

public static class RoomClosureRules
{
    public static async Task<bool> HasClosureAsync(
        this IAppDbContext db,
        int roomId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken
    ) =>
        await db.RoomClosures.AnyAsync(
            c => c.RoomId == roomId && c.EndTime > startTime && c.StartTime < endTime,
            cancellationToken
        );

    public static async Task EnsureRoomOpenAsync(
        this IAppDbContext db,
        int roomId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken
    )
    {
        if (await db.HasClosureAsync(roomId, startTime, endTime, cancellationToken))
            throw new ConflictAppException(Messages.Conflict_ShowtimeOverlapRoomClosure);
    }

    public static async Task<Dictionary<int, List<(DateTime Start, DateTime End)>>> LoadIntervalsAsync(
        this IAppDbContext db,
        IReadOnlyCollection<int> roomIds,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        var rows = await db
            .RoomClosures.AsNoTracking()
            .Where(c => roomIds.Contains(c.RoomId) && c.EndTime > from && c.StartTime < to)
            .Select(c => new
            {
                c.RoomId,
                c.StartTime,
                c.EndTime,
            })
            .ToListAsync(cancellationToken);

        var result = roomIds.ToDictionary(id => id, _ => new List<(DateTime Start, DateTime End)>());
        foreach (var row in rows)
            result[row.RoomId].Add((row.StartTime, row.EndTime));
        return result;
    }

    public static bool Overlaps(
        IEnumerable<(DateTime Start, DateTime End)> intervals,
        DateTime start,
        DateTime end
    ) => intervals.Any(i => i.End > start && i.Start < end);
}
