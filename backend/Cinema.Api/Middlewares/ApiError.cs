namespace Cinema.Api.Middlewares;

public sealed record ApiError(
    int StatusCode,
    string Message,
    IDictionary<string, string[]>? Errors = null,
    LogLevel LogLevel = LogLevel.Warning,
    string? ErrorCode = null,
    object? Payload = null
);
