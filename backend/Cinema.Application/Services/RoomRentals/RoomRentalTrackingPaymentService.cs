using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalTrackingPaymentService(
    IAppDbContext db,
    ITokenService tokenService,
    IRoomRentalTrackingAttemptGuard attemptGuard,
    IRoomRentalPaymentOrderService paymentOrders,
    IOrderCheckoutService checkoutService,
    IOrderGiftCardService giftCardService
) : IRoomRentalTrackingPaymentService
{
    public async Task<RoomRentalPaymentsDto> GetAsync(
        string token,
        CancellationToken cancellationToken
    )
    {
        var requestId = await ResolveRequestIdAsync(token, cancellationToken);
        return await paymentOrders.GetAsync(requestId, cancellationToken);
    }

    public async Task<CheckoutResponse> CheckoutAsync(
        string token,
        RoomRentalPaymentStage stage,
        string paymentMethod,
        CancellationToken cancellationToken
    )
    {
        var order = await EnsurePendingOrderAsync(token, stage, cancellationToken);
        return await checkoutService.CheckoutRoomRentalAsync(
            order.OrderId,
            new CheckoutRequest(paymentMethod),
            cancellationToken
        );
    }

    public async Task<OrderGiftCardResponse> ApplyGiftCardAsync(
        string token,
        RoomRentalPaymentStage stage,
        string code,
        CancellationToken cancellationToken
    )
    {
        var order = await EnsurePendingOrderAsync(token, stage, cancellationToken);
        return await giftCardService.ApplyAsync(
            order.OrderId,
            OrderPromotionCaller.RoomRental,
            code,
            cancellationToken
        );
    }

    private async Task<RoomRentalPaymentOrderDto> EnsurePendingOrderAsync(
        string token,
        RoomRentalPaymentStage stage,
        CancellationToken cancellationToken
    )
    {
        var requestId = await ResolveRequestIdAsync(token, cancellationToken);
        var order = await paymentOrders.EnsureOrderAsync(requestId, stage, cancellationToken);
        if (order.Status != OrderStatus.Pending.ToDtoStatus())
            throw new BusinessRuleException(Messages.Rule_RoomRentalOrderNotPending);
        return order;
    }

    private async Task<int> ResolveRequestIdAsync(string token, CancellationToken cancellationToken)
    {
        await attemptGuard.EnsureNotLockedAsync(cancellationToken);

        var hash = tokenService.HashOpaqueToken(token.Trim());
        var now = DateTime.UtcNow;
        var requestId = await db
            .RoomRentalRequests.AsNoTracking()
            .Where(r => r.TrackingTokenHash == hash && r.TrackingTokenExpiresAt > now)
            .Select(r => (int?)r.RoomRentalRequestId)
            .FirstOrDefaultAsync(cancellationToken);
        if (requestId is { } found)
            return found;

        await attemptGuard.RecordFailureAsync(cancellationToken);
        throw new NotFoundException(Messages.NotFound_RoomRentalTracking);
    }
}
