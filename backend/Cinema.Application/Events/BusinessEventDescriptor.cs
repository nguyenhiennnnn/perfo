using System.Text.Json;
using System.Text.Json.Serialization;
using Cinema.Application.Interfaces.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cinema.Application.Events;

public sealed record ResolvedEventHandlers(
    IReadOnlyList<Func<CancellationToken, Task>> InTransaction,
    IReadOnlyList<string> Background
)
{
    public bool IsEmpty => InTransaction.Count == 0 && Background.Count == 0;
}

public interface IBusinessEventDescriptor
{
    string Name { get; }
    string Serialize(IBusinessEvent businessEvent);
    ResolvedEventHandlers Resolve(IServiceProvider services, IBusinessEvent businessEvent);
    Task<bool> RunBackgroundHandlerAsync(
        IServiceProvider services,
        string handlerName,
        string payload,
        CancellationToken cancellationToken
    );
}

internal sealed class BusinessEventDescriptor<TEvent> : IBusinessEventDescriptor
    where TEvent : IBusinessEvent
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public string Name => typeof(TEvent).Name;

    public string Serialize(IBusinessEvent businessEvent) =>
        JsonSerializer.Serialize((TEvent)businessEvent, JsonOptions);

    public ResolvedEventHandlers Resolve(IServiceProvider services, IBusinessEvent businessEvent)
    {
        var typed = (TEvent)businessEvent;
        var inTransaction = services
            .GetServices<IInTransactionEventHandler<TEvent>>()
            .Select<IInTransactionEventHandler<TEvent>, Func<CancellationToken, Task>>(handler =>
                ct => handler.HandleAsync(typed, ct)
            )
            .ToList();
        var background = services
            .GetServices<IBackgroundEventHandler<TEvent>>()
            .Select(handler => handler.Name)
            .Distinct()
            .ToList();
        return new ResolvedEventHandlers(inTransaction, background);
    }

    public async Task<bool> RunBackgroundHandlerAsync(
        IServiceProvider services,
        string handlerName,
        string payload,
        CancellationToken cancellationToken
    )
    {
        var handler = services
            .GetServices<IBackgroundEventHandler<TEvent>>()
            .FirstOrDefault(h => h.Name == handlerName);
        if (handler is null)
            return false;

        var businessEvent =
            JsonSerializer.Deserialize<TEvent>(payload, JsonOptions)
            ?? throw new JsonException(payload);
        await handler.HandleAsync(businessEvent, cancellationToken);
        return true;
    }
}
