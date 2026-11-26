using Cinema.Api.Extensions;
using Cinema.Application.DTOs.Memberships;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Shared.Constants;
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
    IMembershipFreeTicketQueryService freeTicketQueryService
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
}
