using Cinema.Application.DTOs.SalesStats;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/sales-stats")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminSalesStatsController(ISalesStatsRebuildService rebuildService)
    : AppControllerBase
{
    [HttpPost("rebuild")]
    public async Task<IActionResult> Rebuild(
        [FromBody] RebuildSalesStatsRequest request,
        CancellationToken cancellationToken
    )
    {
        var accepted = await rebuildService.EnqueueAsync(
            CurrentActor,
            request.FromDate,
            request.ToDate,
            cancellationToken
        );
        return StatusCode(StatusCodes.Status202Accepted, accepted);
    }
}
