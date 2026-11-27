namespace Cinema.Shared.Exceptions;

public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, string? errorCode = null)
        : base(StatusCodes.Status422UnprocessableEntity, message, errorCode: errorCode) { }
}
