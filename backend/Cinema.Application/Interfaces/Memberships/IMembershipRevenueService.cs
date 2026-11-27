namespace Cinema.Application.Interfaces.Memberships;

public sealed record MembershipRevenueSettlement(
    int TotalMonths,
    int UnrecognizedMonths,
    decimal UnrecognizedAmount
);

public interface IMembershipRevenueService
{
    Task<int> RecognizeDueAsync(CancellationToken cancellationToken);

    Task<MembershipRevenueSettlement> SettleForRefundAsync(
        int subscriptionId,
        CancellationToken cancellationToken
    );
}
