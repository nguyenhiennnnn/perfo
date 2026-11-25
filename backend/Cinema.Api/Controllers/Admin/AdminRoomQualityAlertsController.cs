using Cinema.Application.Interfaces.Surveys;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/room-quality-alerts")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminRoomQualityAlertsController(IRoomQualityAlertQueryService alertQueryService)
    : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default
    ) => Ok(await alertQueryService.GetAsync(activeOnly, cancellationToken));
}
