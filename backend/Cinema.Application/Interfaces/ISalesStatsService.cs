namespace Cinema.Application.Interfaces;

public interface ISalesStatsService
{
    Task RefreshShowtimesAsync(
        IReadOnlyCollection<int> showtimeIds,
        CancellationToken cancellationToken
    );

    Task RefreshPaymentDaysAsync(
        IReadOnlyCollection<DateOnly> businessDays,
        CancellationToken cancellationToken
    );

    Task RefreshForOrderAsync(int orderId, CancellationToken cancellationToken);

    Task RefreshRecentAsync(CancellationToken cancellationToken);

    Task<int> RebuildDayAsync(DateOnly businessDay, CancellationToken cancellationToken);
}
