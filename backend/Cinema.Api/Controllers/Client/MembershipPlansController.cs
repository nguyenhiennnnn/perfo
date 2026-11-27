using Cinema.Api.Extensions;
using Cinema.Application.DTOs.Memberships;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Interfaces.Orders;
using Cinema.Domain.Enums;
using Cinema.Api.Filters;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/membership-plans")]
public class MembershipPlansController(
    IMembershipPlanService planService,
    IMembershipPurchaseService purchaseService,
    IMembershipSubscriptionQueryService subscriptionQueryService,
    IMembershipFreeTicketQueryService freeTicketQueryService,
    IMembershipRefundService refundService
) : AppControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<MembershipPlanDto>>> GetActive(
        CancellationToken cancellationToken
    ) => Ok(await planService.GetActiveAsync(cancellationToken));

    [HttpPost("{id:int}/purchase")]
    [Authorize]
    [EnableRateLimiting(RateLimitingExtensions.BookingHoldPolicy)]
    public async Task<ActionResult<HoldOrderResponse>> Purchase(
        int id,
        CancellationToken cancellationToken
    )
    {
        var result = await purchaseService.PurchaseAsync(CurrentUserId, id, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("subscriptions/mine")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<MembershipSubscriptionDto>>> GetMine(
        CancellationToken cancellationToken
    ) => Ok(await subscriptionQueryService.GetMineAsync(CurrentUserId, cancellationToken));

    [HttpGet("subscriptions/mine/free-tickets")]
    [Authorize]
    public async Task<ActionResult<MembershipFreeTicketBalanceDto>> GetMyFreeTickets(
        CancellationToken cancellationToken
    ) => Ok(await freeTicketQueryService.GetMineAsync(CurrentUserId, cancellationToken));

    [HttpPost("subscriptions/{id:int}/cancel")]
    [Authorize]
    [SuccessMessage(nameof(Messages.Success_MembershipSubscriptionRefunded))]
    public async Task<ActionResult<MembershipRefundResultDto>> Cancel(
        int id,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await refundService.RefundAsync(
                id,
                new CancelActor(CancelActorKind.Customer, CurrentUserId),
                MembershipRefundReason.CustomerRequest,
                null,
                cancellationToken
            )
        );
}
