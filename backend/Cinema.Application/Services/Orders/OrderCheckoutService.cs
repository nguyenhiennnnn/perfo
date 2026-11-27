using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.GiftCards;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Ledger;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Payments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Application.Services.Orders;

public class OrderCheckoutService(
    IAppDbContext db,
    IPaymentLedgerService ledger,
    IGiftCardHoldService giftCardHolds,
    IEnumerable<IPaymentGateway> paymentGateways,
    ILogger<OrderCheckoutService> logger
) : IOrderCheckoutService
{
    public async Task<CheckoutResponse> CheckoutAsync(
        int userId,
        int orderId,
        CheckoutRequest request,
        CancellationToken cancellationToken
    )
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order.UserId != userId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);
        return await CheckoutCoreAsync(order, request, userId, cancellationToken);
    }

    public async Task<CheckoutResponse> CheckoutRoomRentalAsync(
        int orderId,
        CheckoutRequest request,
        CancellationToken cancellationToken
    )
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order.Type != OrderType.RoomRental)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);
        return await CheckoutCoreAsync(order, request, order.UserId, cancellationToken);
    }

    private async Task<Order> LoadOrderAsync(int orderId, CancellationToken cancellationToken) =>
        await db
            .Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_Order);

    private async Task<CheckoutResponse> CheckoutCoreAsync(
        Order order,
        CheckoutRequest request,
        int? performedByUserId,
        CancellationToken cancellationToken
    )
    {
        var orderId = order.OrderId;
        if (order.Status != OrderStatus.Pending || order.ExpiresAt <= DateTime.UtcNow)
            throw new BusinessRuleException(Messages.Rule_BookingNotPendingOrExpired);

        var gateway =
            paymentGateways.FindByMethod(request.PaymentMethod)
            ?? throw new BusinessRuleException(
                string.Format(Messages.Rule_PaymentGatewayNotConfigured, request.PaymentMethod)
            );

        var method = EnumConverters.ParseEnum<PaymentMethod>(
            request.PaymentMethod,
            "paymentMethod"
        );
        var transactionCode = CodeGenerator.GeneratePaymentTransactionId(order.OrderCode);
        var nowUtc = DateTime.UtcNow;
        var createdAt = new DateTime(
            nowUtc.Ticks - nowUtc.Ticks % TimeSpan.TicksPerSecond,
            DateTimeKind.Utc
        );

        // Pha 1: giao dịch ngắn, giữ khóa đơn. Số cần thu, việc vô hiệu charge Pending cũ và việc tạo
        // charge Pending mới phải cùng một khóa, nếu không RecalculateAsync (đổi mã khuyến mãi) hay
        // một lần checkout khác có thể chen giữa và để lại charge lệch số với FinalAmount.
        PaymentTransaction charge = null!;
        await db.InTransactionAsync(
            async () =>
            {
                await db.LockOrderAsync(orderId, cancellationToken);

                var current =
                    await db
                        .Orders.AsNoTracking()
                        .Where(o => o.OrderId == orderId)
                        .Select(o => new { o.Status, o.ExpiresAt })
                        .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException(Messages.NotFound_Order);
                if (current.Status != OrderStatus.Pending || current.ExpiresAt <= DateTime.UtcNow)
                    throw new BusinessRuleException(Messages.Rule_BookingNotPendingOrExpired);

                var giftCardReserved = await giftCardHolds.GetReservedAmountAsync(
                    orderId,
                    cancellationToken
                );
                var outstanding =
                    (await ledger.GetSummaryAsync(orderId, cancellationToken)).OutstandingAmount
                    - giftCardReserved;
                if (outstanding <= 0m)
                    throw new BusinessRuleException(
                        giftCardReserved > 0m
                            ? Messages.Rule_GiftCardOrderFullyCovered
                            : Messages.Rule_BookingNotPendingOrExpired
                    );

                await db
                    .PaymentTransactions.Where(t =>
                        t.OrderId == orderId
                        && t.Type == PaymentTransactionType.Charge
                        && t.Status == PaymentStatus.Pending
                    )
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(t => t.Status, PaymentStatus.Failed),
                        cancellationToken
                    );

                charge = new PaymentTransaction
                {
                    OrderId = orderId,
                    Type = PaymentTransactionType.Charge,
                    Method = method,
                    Amount = outstanding,
                    TransactionCode = transactionCode,
                    Status = PaymentStatus.Pending,
                    CreatedAt = createdAt,
                    PerformedByUserId = performedByUserId,
                };
                db.PaymentTransactions.Add(charge);
                await db.SaveChangesAsync(cancellationToken);
            },
            cancellationToken
        );

        // Pha 2: gọi cổng ngoài giao dịch (có thể chậm, không được giữ khóa DB). Charge Pending đã có
        // trên sổ trước khi khách nhận link nên IPN đến sớm vẫn khớp được. Cổng lỗi thì đánh Failed
        // ngay để không để lại charge Pending không có link.
        CreatePaymentResult gatewayResult;
        try
        {
            gatewayResult = await gateway.CreatePaymentAsync(
                new CreatePaymentRequest(
                    orderId,
                    charge.Amount,
                    string.Format(Messages.Payment_BookingDescription, order.OrderCode),
                    transactionCode,
                    createdAt
                ),
                cancellationToken
            );
        }
        catch
        {
            await FailChargeAsync(charge.PaymentTransactionId);
            throw;
        }

        logger.LogInformation(
            "PaymentCreated PaymentTransactionId {PaymentTransactionId} OrderId {OrderId} Method {Method}",
            charge.PaymentTransactionId,
            orderId,
            request.PaymentMethod
        );

        return new CheckoutResponse(gatewayResult.PaymentUrl, charge.PaymentTransactionId);
    }

    private async Task FailChargeAsync(int chargeId)
    {
        try
        {
            await db
                .PaymentTransactions.Where(t =>
                    t.PaymentTransactionId == chargeId && t.Status == PaymentStatus.Pending
                )
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(t => t.Status, PaymentStatus.Failed),
                    CancellationToken.None
                );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CheckoutFailChargeError PaymentTransactionId {Id}", chargeId);
        }
    }
}
