namespace Cinema.Application.Interfaces.CounterShifts;

public interface IUnclosedShiftAlertService
{
    Task<int> RunAsync(CancellationToken cancellationToken);
}
