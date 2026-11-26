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
        AddMonths(fromUtc, DurationMonths(period));

    public static DateTime AddMonths(DateTime fromUtc, int months) =>
        BusinessTime.ToUtc(
            DateTime.SpecifyKind(
                BusinessTime.ToLocal(fromUtc).AddMonths(months),
                DateTimeKind.Unspecified
            )
        );

    public static (int Index, DateTime Start, DateTime End) MonthPeriodAt(
        DateTime validFrom,
        DateTime validTo,
        DateTime now
    )
    {
        var index = 0;
        while (AddMonths(validFrom, index + 1) <= now)
            index++;

        var end = AddMonths(validFrom, index + 1);
        return (index, AddMonths(validFrom, index), end > validTo ? validTo : end);
    }
}
