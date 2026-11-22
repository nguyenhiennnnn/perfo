namespace Cinema.Application.Interfaces;

public enum OrderConfirmationResult
{
    Confirmed,
    AlreadyConfirmed,
    AwaitingPayment,
    NotPayable,
}

public interface IOrderConfirmationService
{
    Task<OrderConfirmationResult> TryConfirmAsync(
        int orderId,
        CancellationToken cancellationToken
    );
}
