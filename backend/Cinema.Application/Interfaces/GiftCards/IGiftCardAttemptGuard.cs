namespace Cinema.Application.Interfaces.GiftCards;

public interface IGiftCardAttemptGuard
{
    Task EnsureNotLockedAsync(CancellationToken cancellationToken);

    Task RecordFailureAsync(CancellationToken cancellationToken);
}
