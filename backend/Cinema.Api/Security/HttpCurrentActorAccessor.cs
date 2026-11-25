using System.Security.Claims;
using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces.Auth;
using Cinema.Shared.Constants;

namespace Cinema.Api.Security;

public sealed class HttpCurrentActorAccessor(IHttpContextAccessor httpContextAccessor)
    : ICurrentActorAccessor
{
    public ActorContext? Current
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            var user = context?.User;
            if (user is null || !int.TryParse(user.FindFirstValue(AppClaimTypes.UserId), out var userId))
                return null;

            return new ActorContext(
                userId,
                user.FindFirstValue(AppClaimTypes.Role) ?? string.Empty,
                context!.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers.UserAgent.ToString()
            );
        }
    }
}

public static class CurrentActorExtensions
{
    public static IServiceCollection AddCurrentActorAccessor(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentActorAccessor, HttpCurrentActorAccessor>();
        return services;
    }
}
