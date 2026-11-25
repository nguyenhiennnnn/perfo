using Cinema.Application.Interfaces.Loyalty;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/loyalty/referrals")]
[Authorize]
public class ReferralsController(IReferralService referralService) : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        Ok(await referralService.GetMineAsync(CurrentUserId, cancellationToken));
}
