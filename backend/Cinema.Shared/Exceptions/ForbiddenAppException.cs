namespace Cinema.Shared.Exceptions;

public class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message, string? errorCode = null, object? payload = null)
        : base(StatusCodes.Status403Forbidden, message, errorCode: errorCode, payload: payload) { }
}
