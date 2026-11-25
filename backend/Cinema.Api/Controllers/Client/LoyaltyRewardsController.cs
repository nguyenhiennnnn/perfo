using Cinema.Api.Filters;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/loyalty/rewards")]
[Authorize]
public class LoyaltyRewardsController(
    ILoyaltyRewardCatalogService catalogService,
    ILoyaltyRewardRedemptionService redemptionService
) : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRedeemable(CancellationToken cancellationToken) =>
        Ok(await catalogService.GetRedeemableAsync(cancellationToken));

    [HttpPost("{id:int}/redeem")]
    [SuccessMessage(nameof(Messages.Success_LoyaltyRewardRedeemed))]
    public async Task<IActionResult> Redeem(int id, CancellationToken cancellationToken) =>
        Ok(await redemptionService.RedeemAsync(CurrentUserId, id, cancellationToken));
}
