namespace Cinema.Application.Interfaces.Bundles;

public interface IBundleViewTracker
{
    Task RecordAsync(
        int userId,
        int showtimeId,
        IReadOnlyCollection<int> bundleIds,
        CancellationToken cancellationToken
    );
}
