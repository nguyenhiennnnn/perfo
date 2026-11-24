using Cinema.Application.Utils;

namespace Cinema.Application.Services.Tax;

public static class VatCalculator
{
    public static VatSplit Split(decimal grossAmount, decimal ratePercent)
    {
        var gross = OrderPricing.RoundToDong(grossAmount);
        if (ratePercent <= 0m)
            return new VatSplit(gross, gross, 0m, 0m);

        var tax = OrderPricing.RoundToDong(gross * ratePercent / (100m + ratePercent));
        return new VatSplit(gross, gross - tax, tax, ratePercent);
    }
}
