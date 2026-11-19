using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Payments;

internal static class GatewayHttp
{
    public const int SnippetMaxLength = 300;

    private const string UserAgent =
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) "
        + "Chrome/124.0.0.0 Safari/537.36 PerfoCinema/1.0";

    public static void Configure(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public static HttpMessageHandler CreateHandler() =>
        new SocketsHttpHandler { AllowAutoRedirect = false };

    public static IHttpClientBuilder AddGatewayHttpClient<TClient>(this IServiceCollection services)
        where TClient : class =>
        services
            .AddHttpClient<TClient>(Configure)
            .ConfigurePrimaryHttpMessageHandler(CreateHandler);

    public static async Task<T> PostJsonAsync<T>(
        HttpClient client,
        string url,
        object payload,
        JsonSerializerOptions jsonOptions,
        ILogger logger,
        string gatewayName,
        CancellationToken cancellationToken
    )
        where T : class
    {
        using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var status = (int)response.StatusCode;

        if (!LooksLikeJson(body))
        {
            var location = response.Headers.Location?.ToString();
            logger.LogError(
                "{Gateway}NonJsonResponse Url {Url} Status {Status} ContentType {ContentType} Location {Location} Body {Body}",
                gatewayName,
                url,
                status,
                response.Content.Headers.ContentType?.MediaType,
                location,
                Truncate(body)
            );
            var reason = response.StatusCode is >= HttpStatusCode.MultipleChoices and < HttpStatusCode.BadRequest
                ? $"bị chuyển hướng (HTTP {status}) tới {location ?? "?"} — kiểm tra lại URL API"
                : $"trả về HTTP {status} với nội dung không phải JSON (có thể bị chặn bởi firewall/IP của server hoặc sai URL API)";
            throw new GatewayHttpException($"{gatewayName} {reason}");
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(body, jsonOptions);
            if (result is null)
                throw new GatewayHttpException(
                    $"{gatewayName} trả về dữ liệu rỗng (HTTP {status})"
                );
            return result;
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "{Gateway}InvalidJson Url {Url} Status {Status} Body {Body}",
                gatewayName,
                url,
                status,
                Truncate(body)
            );
            throw new GatewayHttpException(
                $"{gatewayName} trả về JSON không hợp lệ (HTTP {status})"
            );
        }
    }

    private static bool LooksLikeJson(string body)
    {
        var trimmed = body.AsSpan().TrimStart();
        return trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '[');
    }

    private static string Truncate(string body)
    {
        var singleLine = body.ReplaceLineEndings(" ").Trim();
        return singleLine.Length <= SnippetMaxLength
            ? singleLine
            : singleLine[..SnippetMaxLength] + "…";
    }
}

internal sealed class GatewayHttpException(string message) : Exception(message);
