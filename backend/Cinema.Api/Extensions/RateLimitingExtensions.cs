using System.Threading.RateLimiting;
using Cinema.Application.Options;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string BookingHoldPolicy = "booking-hold";
    public const string ExportPolicy = "export";
    public const string ImportPolicy = "import";

    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var rateLimitOptions = configuration.BindSection<RateLimitOptions>(
            RateLimitOptions.SectionName
        );

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                    context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    );
                }

                context
                    .HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(RateLimitingExtensions).FullName!)
                    .LogWarning(
                        "RateLimitRejected Policy {Policy} Partition {Partition} Method {Method} Path {Path}",
                        context.HttpContext.GetEndpoint()
                            ?.Metadata.GetMetadata<EnableRateLimitingAttribute>()
                            ?.PolicyName ?? "global",
                        PartitionKey(context.HttpContext),
                        context.HttpContext.Request.Method,
                        context.HttpContext.Request.Path.Value
                    );

                return ValueTask.CompletedTask;
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: ClientIp(httpContext),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitOptions.GlobalPermitLimit,
                            Window = TimeSpan.FromSeconds(rateLimitOptions.GlobalWindowSeconds),
                            SegmentsPerWindow = rateLimitOptions.GlobalSegmentsPerWindow,
                            QueueLimit = 0,
                        }
                    )
            );

            options.AddPolicy(
                AuthPolicy,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ClientIp(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitOptions.AuthPermitLimit,
                            Window = TimeSpan.FromSeconds(rateLimitOptions.AuthWindowSeconds),
                            QueueLimit = 0,
                        }
                    )
            );

            options.AddPolicy(
                BookingHoldPolicy,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.Identity?.Name ?? ClientIp(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitOptions.BookingHoldPermitLimit,
                            Window = TimeSpan.FromSeconds(
                                rateLimitOptions.BookingHoldWindowSeconds
                            ),
                            QueueLimit = 0,
                        }
                    )
            );

            options.AddPolicy(
                ExportPolicy,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: PartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitOptions.ExportPermitLimit,
                            Window = TimeSpan.FromSeconds(rateLimitOptions.ExportWindowSeconds),
                            QueueLimit = 0,
                        }
                    )
            );

            options.AddPolicy(
                ImportPolicy,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: PartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitOptions.ImportPermitLimit,
                            Window = TimeSpan.FromSeconds(rateLimitOptions.ImportWindowSeconds),
                            QueueLimit = 0,
                        }
                    )
            );
        });
    }

    private static string PartitionKey(HttpContext httpContext) =>
        httpContext.User.FindFirst(AppClaimTypes.UserId)?.Value is { Length: > 0 } userId
            ? $"user:{userId}"
            : $"ip:{ClientIp(httpContext)}";

    private static string ClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
