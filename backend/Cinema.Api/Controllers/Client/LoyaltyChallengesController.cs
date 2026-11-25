using Cinema.Application.Interfaces.Loyalty;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/loyalty/challenges")]
[Authorize]
public class LoyaltyChallengesController(ILoyaltyChallengeQueryService queryService)
    : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        Ok(await queryService.GetMineAsync(CurrentUserId, cancellationToken));
}
