using Cinema.Application.DTOs.Dashboard;
using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/dashboard")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminDashboardController(
    IDashboardService dashboardService,
    IOccupancyAnalyticsService occupancyService
) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("insights")]
    public async Task<IActionResult> GetInsights(
        [FromQuery] int days,
        CancellationToken cancellationToken
    )
    {
        var effectiveDays = days <= 0 ? 7 : days;
        var result = await dashboardService.GetInsightsAsync(effectiveDays, cancellationToken);
        return Ok(result);
    }

    [HttpGet("occupancy")]
    public async Task<IActionResult> GetOccupancy(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] OccupancyGroupBy groupBy,
        [FromQuery] ShowtimeKind[]? kind,
        CancellationToken cancellationToken
    )
    {
        var result = await occupancyService.GetAsync(from, to, groupBy, kind, cancellationToken);
        return Ok(result);
    }
}
