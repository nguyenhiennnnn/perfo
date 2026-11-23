using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Resources;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class CounterComboSaleService(
    ICounterOrderService counterOrders,
    ICounterPaymentService counterPayments,
    IOrderReleaseService releaseService,
    ILogger<CounterComboSaleService> logger
) : ICounterComboSaleService
{
    public async Task<CounterComboSaleResponse> SellAsync(
        int employeeId,
        int employeeUserId,
        CounterComboSaleRequest request,
        CancellationToken cancellationToken
    )
    {
        var customerName = string.IsNullOrWhiteSpace(request.CustomerName)
            ? Messages.Common_WalkInCustomer
            : request.CustomerName.Trim();

        var order = await counterOrders.CreateComboOrderAsync(
            employeeId,
            employeeUserId,
            new CounterComboOrderRequest(
                request.Combos,
                request.UserId,
                customerName,
                request.CustomerPhone
            ),
            cancellationToken
        );

        CounterCheckoutResponse checkout;
        try
        {
            checkout = await counterPayments.PayAsync(
                order.OrderId,
                employeeUserId,
                new CounterCheckoutRequest(request.PaymentMethod, null, request.AmountReceived),
                cancellationToken
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var released = await releaseService.CancelPendingAsync(
                order.OrderId,
                CancelReason.PaymentError,
                CancelledBy.System,
                CancellationToken.None
            );
            logger.LogWarning(
                "CounterComboSalePaymentFailed OrderId {OrderId} Released {Released}",
                order.OrderId,
                released
            );
            throw;
        }

        return new CounterComboSaleResponse(order with { Status = checkout.Status }, checkout);
    }
}
