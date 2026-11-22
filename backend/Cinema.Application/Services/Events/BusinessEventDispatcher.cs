using Cinema.Application.Events;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Events;

public class BusinessEventDispatcher(IAppDbContext db, IServiceProvider services)
    : IBusinessEventDispatcher
{
    public async Task PublishAsync<TEvent>(TEvent businessEvent, CancellationToken cancellationToken)
        where TEvent : IBusinessEvent
    {
        var descriptor = BusinessEventCatalog.For<TEvent>();
        var handlers = descriptor.Resolve(services, businessEvent);
        if (handlers.IsEmpty)
            return;

        await db.InTransactionAsync(
            async () =>
            {
                foreach (var run in handlers.InTransaction)
                    await run(cancellationToken);

                if (handlers.Background.Count > 0)
                    db.OutboxMessages.Add(BuildMessage(descriptor, businessEvent, handlers));

                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );
    }

    private static OutboxMessage BuildMessage(
        IBusinessEventDescriptor descriptor,
        IBusinessEvent businessEvent,
        ResolvedEventHandlers handlers
    )
    {
        var now = DateTime.UtcNow;
        return new OutboxMessage
        {
            EventType = descriptor.Name,
            Payload = descriptor.Serialize(businessEvent),
            OccurredAt = businessEvent.OccurredAt,
            CreatedAt = now,
            Deliveries =
            [
                .. handlers.Background.Select(name => new OutboxDelivery
                {
                    HandlerName = name,
                    Status = OutboxDeliveryStatus.Pending,
                    NextAttemptAt = now,
                    CreatedAt = now,
                }),
            ],
        };
    }
}
