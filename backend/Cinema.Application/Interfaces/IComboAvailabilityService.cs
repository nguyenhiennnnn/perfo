namespace Cinema.Application.Interfaces;

public interface IComboAvailabilityService
{
    Task EnsureAvailableAsync(
        IReadOnlyCollection<(int ComboId, int Quantity)> selections,
        CancellationToken cancellationToken
    );
}
