namespace Cinema.Shared.Exceptions;

public class PromotionRejectedException(string message, string errorCode)
    : AppException(StatusCodes.Status422UnprocessableEntity, message, errorCode: errorCode);
