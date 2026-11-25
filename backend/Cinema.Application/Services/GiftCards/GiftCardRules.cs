using System.Globalization;
using Cinema.Application.Caching;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.GiftCards;

public static class GiftCardRules
{
    public static decimal EnsureAmountAllowed(CinemaSettingsSnapshot settings, decimal amount)
    {
        var faceValue = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (faceValue <= 0)
            throw new BusinessRuleException(Messages.Rule_GiftCardAmountInvalid);
        if (faceValue < settings.GiftCardMinAmount || faceValue > settings.GiftCardMaxAmount)
            throw new BusinessRuleException(
                string.Format(
                    Messages.Rule_GiftCardAmountOutOfRange,
                    FormatMoney(settings.GiftCardMinAmount),
                    FormatMoney(settings.GiftCardMaxAmount)
                )
            );

        return faceValue;
    }

    public static string FormatMoney(decimal amount) =>
        $"{amount.ToString("N0", CultureInfo.GetCultureInfo(RegionalDefaults.Locale))} {Messages.Common_CurrencyVnd}";

    public static string? NormalizeText(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
