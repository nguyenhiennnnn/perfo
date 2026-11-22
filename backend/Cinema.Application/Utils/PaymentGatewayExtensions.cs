using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class PaymentGatewayExtensions
{
    public static IPaymentGateway? FindByMethod(
        this IEnumerable<IPaymentGateway> gateways,
        string methodName
    ) => gateways.FirstOrDefault(g => g.MethodName == methodName);

    public static bool IsGatewayMethod(this PaymentMethod method) =>
        method is PaymentMethod.VNPay or PaymentMethod.Momo or PaymentMethod.Stripe;
}
