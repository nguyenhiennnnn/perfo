using Cinema.Api.Filters;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/gift-cards")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminGiftCardsController(
    IGiftCardDispatchService dispatchService,
    IGiftCardCancellationService cancellationService
) : ControllerBase
{
    [HttpPost("{id:int}/resend")]
    [SuccessMessage(nameof(Messages.Success_GiftCardResent))]
    public async Task<IActionResult> Resend(int id, CancellationToken cancellationToken)
    {
        await dispatchService.ResendAsync(id, cancellationToken);
        return new EmptyResult();
    }

    [HttpPost("{id:int}/cancel")]
    [SuccessMessage(nameof(Messages.Success_GiftCardCancelled))]
    public async Task<ActionResult<CancelGiftCardResponse>> Cancel(
        int id,
        [FromBody] CancelGiftCardRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await cancellationService.CancelAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
