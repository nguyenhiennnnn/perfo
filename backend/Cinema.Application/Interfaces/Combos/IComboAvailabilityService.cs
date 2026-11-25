namespace Cinema.Application.Interfaces.Combos;

public interface IComboAvailabilityService
{
    Task EnsureAvailableAsync(
        IReadOnlyCollection<(int ComboId, int Quantity)> selections,
        CancellationToken cancellationToken
    );
}
