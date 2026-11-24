using Cinema.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Pricing;

public sealed class PaceSalesVelocityProvider(IAppDbContext db) : ISalesVelocityProvider
{
    public async Task<decimal?> GetTicketsPerHourAsync(
        int showtimeId,
        DateTime atUtc,
        CancellationToken cancellationToken
    )
    {
        var startTime = await db
            .Showtimes.AsNoTracking()
            .Where(s => s.ShowtimeId == showtimeId)
            .Select(s => (DateTime?)s.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
        if (startTime is null)
            return null;

        var minutesToStart = (startTime.Value - atUtc).TotalMinutes;
        var reached = await db
            .ShowtimeSalesPaces.AsNoTracking()
            .Where(p => p.ShowtimeId == showtimeId && p.MarkMinutesBeforeStart >= minutesToStart)
            .OrderBy(p => p.MarkMinutesBeforeStart)
            .Take(2)
            .Select(p => new { p.MarkMinutesBeforeStart, p.TicketsSold })
            .ToListAsync(cancellationToken);
        if (reached.Count < 2)
            return null;

        var latest = reached[0];
        var previous = reached[1];
        var hours = (previous.MarkMinutesBeforeStart - latest.MarkMinutesBeforeStart) / 60m;
        if (hours <= 0)
            return null;

        return Math.Max(0m, (latest.TicketsSold - previous.TicketsSold) / hours);
    }
}
