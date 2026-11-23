using Cinema.Api.Extensions;
using Cinema.Api.Filters;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/bookings")]
[Authorize]
public class BookingsController(
    IOrderHoldService bookingHoldService,
    IOrderQueryService orderQueryService,
    IOrderCheckoutService bookingCheckoutService,
    IOrderCancellationService orderCancellationService,
    IOrderTicketPdfService orderTicketPdfService,
    IOrderPromotionService orderPromotionService
) : AppControllerBase
{
    [HttpPost("hold")]
    [EnableRateLimiting(RateLimitingExtensions.BookingHoldPolicy)]
    public async Task<ActionResult<HoldOrderResponse>> HoldSeats(
        [FromBody] HoldSeatsRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await bookingHoldService.HoldSeatsAsync(
            CurrentUserId,
            request,
            cancellationToken
        );
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    public async Task<ActionResult<CursorPageResult<OrderListItemDto>>> GetMyOrders(
        [FromQuery] OrderListFilter filter,
        [FromQuery] string? cursor,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        var result = await orderQueryService.GetMyOrdersAsync(
            CurrentUserId,
            filter,
            cursor,
            ClampLimit(limit),
            cancellationToken
        );
        return Ok(result);
    }

    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrderDetail(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var result = await orderQueryService.GetMyOrderDetailAsync(
            CurrentUserId,
            orderId,
            cancellationToken
        );
        return Ok(result);
    }

    [HttpGet("{orderId:int}/tickets/pdf")]
    public async Task<IActionResult> DownloadTicketsPdf(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var file = await orderTicketPdfService.GenerateForCustomerAsync(
            CurrentUserId,
            orderId,
            cancellationToken
        );
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost("{bookingId:int}/checkout")]
    public async Task<ActionResult<CheckoutResponse>> Checkout(
        int bookingId,
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await bookingCheckoutService.CheckoutAsync(
            CurrentUserId,
            bookingId,
            request,
            cancellationToken
        );
        return Ok(result);
    }

    [HttpPost("{orderId:int}/promotion")]
    [EnableRateLimiting(RateLimitingExtensions.BookingHoldPolicy)]
    [SuccessMessage(nameof(Messages.Success_OrderPromotionApplied))]
    public async Task<ActionResult<OrderPromotionResponse>> ApplyPromotion(
        int orderId,
        [FromBody] ApplyPromotionRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await orderPromotionService.ApplyAsync(
            orderId,
            OrderPromotionCaller.Customer(CurrentUserId),
            request.Code,
            cancellationToken
        );
        return Ok(result);
    }

    [HttpDelete("{orderId:int}/promotion")]
    [SuccessMessage(nameof(Messages.Success_OrderPromotionRemoved))]
    public async Task<ActionResult<OrderPromotionResponse>> RemovePromotion(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var result = await orderPromotionService.RemoveAsync(
            orderId,
            OrderPromotionCaller.Customer(CurrentUserId),
            cancellationToken
        );
        return Ok(result);
    }

    [HttpPost("{bookingId:int}/cancel")]
    [SuccessMessage(nameof(Messages.Success_BookingCancelled))]
    public async Task<ActionResult<CancelOrderResponse>> CancelBooking(
        int bookingId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await orderCancellationService.CancelAsync(
            bookingId,
            new CancelActor(CancelActorKind.Customer, CurrentUserId),
            OrderCancellationRules.ParseReason(request.Reason),
            cancellationToken
        );
        return Ok(CancelOrderResponse.From(result));
    }
}
