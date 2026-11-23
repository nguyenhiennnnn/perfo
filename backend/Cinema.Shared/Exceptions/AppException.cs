namespace Cinema.Shared.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }
    public string? ErrorCode { get; }
    public object? Payload { get; }

    protected AppException(
        int statusCode,
        string message,
        IDictionary<string, string[]>? errors = null,
        string? errorCode = null,
        object? payload = null
    )
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
        ErrorCode = errorCode;
        Payload = payload;
    }
}
