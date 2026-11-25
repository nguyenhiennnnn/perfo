using Cinema.Application.Events;

namespace Cinema.Application.Interfaces.Events;

public interface IBusinessEventDispatcher
{
    Task PublishAsync<TEvent>(TEvent businessEvent, CancellationToken cancellationToken)
        where TEvent : IBusinessEvent;
}
