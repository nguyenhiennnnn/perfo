namespace Cinema.Application.Interfaces.Orders;

public interface IOrderTicketEmailSender
{
    Task<bool> SendAsync(int orderId, CancellationToken cancellationToken);
}
