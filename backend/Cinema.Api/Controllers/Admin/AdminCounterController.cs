using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/bookings")]
[Authorize]
public class AdminCounterController(
    ICounterOrderService counterOrderService,
    ICounterPaymentService counterPaymentService,
    ITicketCheckInService ticketCheckInService,
    ICardCheckService cardCheckService,
    ISupplementOrderService supplementOrderService,
    IOrderPromotionService orderPromotionService
) : AppControllerBase
{
    private int GetCurrentEmployeeId() =>
        CurrentEmployeeIdOrNull
        ?? throw new BusinessRuleException(Messages.Rule_NoEmployeeProfileForCounterBooking);

    [HttpPost("counter-hold")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<CounterOrderResponse>> CounterHoldSeats(
        [FromBody] CounterHoldRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await counterOrderService.CreateTicketOrderAsync(
            GetCurrentEmployeeId(),
            CurrentUserId,
            request,
            cancellationToken
        );
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("counter-combo-order")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<CounterOrderResponse>> CounterComboOrder(
        [FromBody] CounterComboOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await counterOrderService.CreateComboOrderAsync(
            GetCurrentEmployeeId(),
            CurrentUserId,
            request,
            cancellationToken
        );
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("{orderId:int}/counter-checkout")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<CounterCheckoutResponse>> CounterCheckout(
        int orderId,
        [FromBody] CounterCheckoutRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await counterPaymentService.PayAsync(
            orderId,
            CurrentUserId,
            request,
            cancellationToken
        );
        return Ok(result);
    }

    [HttpPost("{orderId:int}/counter-promotion")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<OrderPromotionResponse>> CounterApplyPromotion(
        int orderId,
        [FromBody] ApplyPromotionRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await orderPromotionService.ApplyAsync(
            orderId,
            OrderPromotionCaller.Counter,
            request.Code,
            cancellationToken
        );
        return Ok(result);
    }

    [HttpDelete("{orderId:int}/counter-promotion")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<OrderPromotionResponse>> CounterRemovePromotion(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var result = await orderPromotionService.RemoveAsync(
            orderId,
            OrderPromotionCaller.Counter,
            cancellationToken
        );
        return Ok(result);
    }

    [HttpPost("{orderId:int}/counter-cancel")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<CancelOrderResponse>> CounterCancel(
        int orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await counterOrderService.CancelAsync(
            orderId,
            new CancelActor(OrderCancellationRules.KindForRole(CurrentActor.Role), CurrentUserId),
            OrderCancellationRules.ParseReason(request.Reason),
            cancellationToken
        );
        return Ok(CancelOrderResponse.From(result));
    }

    [HttpPost("checkin")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<CheckinResponse>> Checkin(
        [FromBody] CheckinRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await ticketCheckInService.CheckInAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("card-not-presented")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<RecordCardNotPresentedResponse>> RecordCardNotPresented(
        [FromBody] RecordCardNotPresentedRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await cardCheckService.RecordNotPresentedAsync(
            CurrentUserId,
            request,
            cancellationToken
        );
        return Ok(result);
    }

    [HttpPost("counter-supplement-order")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<CounterOrderResponse>> CounterSupplementOrder(
        [FromBody] CounterSupplementOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await supplementOrderService.CreateAsync(
            GetCurrentEmployeeId(),
            CurrentUserId,
            request,
            cancellationToken
        );
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
