namespace Cinema.Application.Interfaces.Abuse;

public interface ICustomerIdentityLinkService
{
    Task<bool> SharesDeviceWithOtherAsync(int userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> FindLinkedUserIdsAsync(
        int userId,
        CancellationToken cancellationToken
    );
}
