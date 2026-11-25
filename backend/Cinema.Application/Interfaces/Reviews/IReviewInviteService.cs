namespace Cinema.Application.Interfaces.Reviews;

public interface IReviewInviteService
{
    Task<int> SendDueAsync(CancellationToken cancellationToken);
}
