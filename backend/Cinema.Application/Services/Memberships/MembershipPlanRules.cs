using Cinema.Application.Utils;
using Cinema.Domain.Enums;

namespace Cinema.Application.Services.Memberships;

public static class MembershipPlanRules
{
    public const decimal MaxPrice = 100_000_000m;
    public const int MaxFreeTicketsPerMonth = 30;

    public static int DurationMonths(MembershipPlanPeriod period) =>
        period switch
        {
            MembershipPlanPeriod.Monthly => 1,
            MembershipPlanPeriod.Quarterly => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(period)),
        };

    public static DateTime AddPeriod(DateTime fromUtc, MembershipPlanPeriod period) =>
        BusinessTime.ToUtc(
            DateTime.SpecifyKind(
                BusinessTime.ToLocal(fromUtc).AddMonths(DurationMonths(period)),
                DateTimeKind.Unspecified
            )
        );
}
