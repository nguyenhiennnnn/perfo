namespace Cinema.Application.Utils;

public static class DiscountAllocator
{
    public static decimal[] Allocate(decimal amount, IReadOnlyList<decimal> bases)
    {
        var shares = new decimal[bases.Count];
        var total = bases.Where(b => b > 0m).Sum();
        amount = Math.Min(OrderPricing.RoundToDong(amount), total);
        if (amount <= 0m)
            return shares;

        var last = -1;
        for (var i = bases.Count - 1; i >= 0; i--)
        {
            if (bases[i] > 0m)
            {
                last = i;
                break;
            }
        }

        var allocated = 0m;
        for (var i = 0; i < bases.Count; i++)
        {
            if (i == last || bases[i] <= 0m)
                continue;

            shares[i] = Math.Min(bases[i], Math.Floor(amount * bases[i] / total));
            allocated += shares[i];
        }

        var remainder = amount - allocated;
        shares[last] = Math.Min(bases[last], remainder);
        remainder -= shares[last];

        for (var i = last - 1; i >= 0 && remainder > 0m; i--)
        {
            var extra = Math.Min(bases[i] - shares[i], remainder);
            if (extra <= 0m)
                continue;

            shares[i] += extra;
            remainder -= extra;
        }

        return shares;
    }
}
