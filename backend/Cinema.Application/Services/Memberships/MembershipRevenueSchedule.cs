namespace Cinema.Application.Services.Memberships;

public static class MembershipRevenueSchedule
{
    private const int MaxMonths = 120;

    public static int Months(DateTime validFrom, DateTime validTo)
    {
        var months = 1;
        while (months < MaxMonths && MembershipPlanRules.AddMonths(validFrom, months) < validTo)
            months++;
        return months;
    }

    public static (DateTime Start, DateTime End) Period(
        DateTime validFrom,
        DateTime validTo,
        int months,
        int index
    )
    {
        var end = index >= months - 1 ? validTo : MembershipPlanRules.AddMonths(validFrom, index + 1);
        return (MembershipPlanRules.AddMonths(validFrom, index), end);
    }

    public static IReadOnlyList<decimal> Split(decimal price, int months)
    {
        var amounts = new decimal[months];
        var share = decimal.Floor(price / months);
        for (var i = 0; i < months - 1; i++)
            amounts[i] = share;
        amounts[months - 1] = price - share * (months - 1);
        return amounts;
    }
}
