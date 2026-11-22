namespace Cinema.Application.Utils;

public static class OrderPricing
{
    public static decimal RoundToDong(decimal value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);

    public static decimal[] AllocateProportionally(decimal amount, IReadOnlyList<decimal> bases)
    {
        var result = new decimal[bases.Count];
        var total = bases.Sum();
        if (amount <= 0m || total <= 0m)
            return result;

        amount = Math.Min(RoundToDong(amount), total);
        var allocated = 0m;
        for (var i = 0; i < bases.Count; i++)
        {
            result[i] = Math.Min(bases[i], Math.Floor(amount * bases[i] / total));
            allocated += result[i];
        }

        for (var i = 0; allocated < amount && i < bases.Count; i++)
        {
            var extra = Math.Min(bases[i] - result[i], amount - allocated);
            result[i] += extra;
            allocated += extra;
        }

        return result;
    }
}
