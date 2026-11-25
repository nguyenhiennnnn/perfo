namespace Cinema.Application.Interfaces.Reviews;

public interface IReviewRewardService
{
    Task<int> GrantAsync(int userId, int movieId, CancellationToken cancellationToken);
}
