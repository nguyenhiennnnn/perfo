using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Cinema.Application.Interfaces.Events;


namespace Cinema.Application.Events;

public static class BusinessEventHandlerRegistration
{
    public static IServiceCollection AddInTransactionEventHandler<TEvent, THandler>(
        this IServiceCollection services
    )
        where TEvent : IBusinessEvent
        where THandler : class, IInTransactionEventHandler<TEvent>
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IInTransactionEventHandler<TEvent>, THandler>()
        );
        return services;
    }

    public static IServiceCollection AddBackgroundEventHandler<TEvent, THandler>(
        this IServiceCollection services
    )
        where TEvent : IBusinessEvent
        where THandler : class, IBackgroundEventHandler<TEvent>
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IBackgroundEventHandler<TEvent>, THandler>()
        );
        return services;
    }
}
