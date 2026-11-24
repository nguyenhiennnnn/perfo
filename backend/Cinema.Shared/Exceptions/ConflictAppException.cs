namespace Cinema.Shared.Exceptions;

public class ConflictAppException : AppException
{
    public ConflictAppException(string message)
        : base(StatusCodes.Status409Conflict, message) { }

    public ConflictAppException(string message, string errorCode)
        : base(StatusCodes.Status409Conflict, message, errorCode: errorCode) { }
}
