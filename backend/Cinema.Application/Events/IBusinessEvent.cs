namespace Cinema.Application.Events;

public interface IBusinessEvent
{
    DateTime OccurredAt { get; }
}
