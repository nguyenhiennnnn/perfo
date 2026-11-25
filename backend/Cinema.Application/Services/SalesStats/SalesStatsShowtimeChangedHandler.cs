using Cinema.Application.Events;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Interfaces.Events;


namespace Cinema.Application.Services.SalesStats;

public class SalesStatsShowtimeChangedHandler(ISalesStatsService salesStats)
    : IBackgroundEventHandler<ShowtimeEnded>,
        IBackgroundEventHandler<ShowtimeRoomChanged>
{
    public Task HandleAsync(ShowtimeEnded businessEvent, CancellationToken cancellationToken) =>
        salesStats.RefreshShowtimesAsync([businessEvent.ShowtimeId], cancellationToken);

    public Task HandleAsync(
        ShowtimeRoomChanged businessEvent,
        CancellationToken cancellationToken
    ) => salesStats.RefreshShowtimesAsync([businessEvent.ShowtimeId], cancellationToken);
}
