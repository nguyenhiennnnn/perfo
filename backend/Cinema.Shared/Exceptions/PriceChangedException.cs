using Cinema.Shared.Constants;

namespace Cinema.Shared.Exceptions;

public class PriceChangedException(string message, object changes)
    : AppException(
        StatusCodes.Status409Conflict,
        message,
        errorCode: ApiErrorCodes.PriceChangeConfirmationRequired,
        payload: changes
    );
