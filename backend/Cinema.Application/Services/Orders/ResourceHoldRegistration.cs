using Cinema.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cinema.Application.Services.Orders;

public static class ResourceHoldRegistration
{
    public static IServiceCollection AddResourceHold<THold>(this IServiceCollection services)
        where THold : class, IResourceHold
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IResourceHold, THold>());
        return services;
    }
}
