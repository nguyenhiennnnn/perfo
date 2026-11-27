namespace Cinema.Application.Interfaces.Segments;

public interface ICustomerSegmentService
{
    Task RefreshAsync(CancellationToken cancellationToken);
}
