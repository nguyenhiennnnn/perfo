using Cinema.Application.Events;

namespace Cinema.Application.Interfaces.Events;

public interface IInTransactionEventHandler<in TEvent>
    where TEvent : IBusinessEvent
{
    Task HandleAsync(TEvent businessEvent, CancellationToken cancellationToken);
}

public interface IBackgroundEventHandler<in TEvent>
    where TEvent : IBusinessEvent
{
    string Name => GetType().Name;

    Task HandleAsync(TEvent businessEvent, CancellationToken cancellationToken);
}
