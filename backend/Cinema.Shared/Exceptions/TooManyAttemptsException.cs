namespace Cinema.Shared.Exceptions;

public class TooManyAttemptsException(string message, string errorCode, object? payload = null)
    : AppException(StatusCodes.Status429TooManyRequests, message, errorCode: errorCode, payload: payload);
