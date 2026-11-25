namespace Cinema.Application.Interfaces;

public interface IGiftCardAttemptGuard
{
    Task EnsureNotLockedAsync(CancellationToken cancellationToken);

    Task RecordFailureAsync(CancellationToken cancellationToken);
}
