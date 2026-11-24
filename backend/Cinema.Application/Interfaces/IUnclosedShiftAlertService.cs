namespace Cinema.Application.Interfaces;

public interface IUnclosedShiftAlertService
{
    Task<int> RunAsync(CancellationToken cancellationToken);
}
