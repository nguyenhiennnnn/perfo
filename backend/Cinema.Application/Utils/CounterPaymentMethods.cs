using Cinema.Domain.Enums;
using Cinema.Shared.Constants;

namespace Cinema.Application.Utils;

public static class CounterPaymentMethods
{
    public static bool IsCash(string? value) => Matches(value, PaymentMethodNames.Cash);

    public static bool IsCard(string? value) => Matches(value, PaymentMethodNames.Card);

    public static bool IsSupported(string? value) => IsCash(value) || IsCard(value);

    public static bool TryParse(string? value, out PaymentMethod method)
    {
        if (IsCash(value))
        {
            method = PaymentMethod.Cash;
            return true;
        }

        if (IsCard(value))
        {
            method = PaymentMethod.Card;
            return true;
        }

        method = default;
        return false;
    }

    private static bool Matches(string? value, string name) =>
        string.Equals(value?.Trim(), name, StringComparison.OrdinalIgnoreCase);
}
