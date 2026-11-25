using Cinema.Api.Filters;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/gift-cards")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminGiftCardsController(IGiftCardDispatchService dispatchService) : ControllerBase
{
    [HttpPost("{id:int}/resend")]
    [SuccessMessage(nameof(Messages.Success_GiftCardResent))]
    public async Task<IActionResult> Resend(int id, CancellationToken cancellationToken)
    {
        await dispatchService.ResendAsync(id, cancellationToken);
        return new EmptyResult();
    }
}
