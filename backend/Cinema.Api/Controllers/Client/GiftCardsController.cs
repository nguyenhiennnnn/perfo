using Cinema.Api.Extensions;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/gift-cards")]
[Authorize]
public class GiftCardsController(IGiftCardPurchaseService purchaseService) : AppControllerBase
{
    [HttpPost("purchase")]
    [EnableRateLimiting(RateLimitingExtensions.BookingHoldPolicy)]
    public async Task<ActionResult<HoldOrderResponse>> Purchase(
        [FromBody] PurchaseGiftCardRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await purchaseService.PurchaseAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
