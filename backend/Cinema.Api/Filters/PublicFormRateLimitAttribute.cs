using Cinema.Application.Interfaces.Abuse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cinema.Api.Filters;

public sealed class PublicFormRateLimitAttribute : TypeFilterAttribute
{
    public PublicFormRateLimitAttribute(string form)
        : base(typeof(PublicFormRateLimitFilter))
    {
        Arguments = [form];
    }
}

public sealed class PublicFormRateLimitFilter(string form, IPublicFormRateLimiter rateLimiter)
    : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        await rateLimiter.EnsureAllowedAsync(form, context.HttpContext.RequestAborted);
        await next();
    }
}
