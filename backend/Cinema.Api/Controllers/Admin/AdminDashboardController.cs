using Cinema.Application.DTOs.Dashboard;
using Cinema.Application.Interfaces.Bookings;
using Cinema.Application.Interfaces.Combos;
using Cinema.Application.Interfaces.Dashboard;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Interfaces.Showtimes;
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
    IOccupancyAnalyticsService occupancyService,
    IComboAnalyticsService comboService,
    ICancellationAnalyticsService cancellationService,
    ISeatHeatmapService seatHeatmapService,
    IScheduleAnalyticsService scheduleService,
    IScheduleSuggestionService suggestionService,
    IScheduleSuggestionApplyService suggestionApplyService
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

    [HttpGet("combo-revenue")]
    public async Task<IActionResult> GetComboRevenue(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken
    )
    {
        var result = await comboService.GetAsync(from, to, cancellationToken);
        return Ok(result);
    }

    [HttpGet("cancellations")]
    public async Task<IActionResult> GetCancellations(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken
    )
    {
        var result = await cancellationService.GetAsync(from, to, cancellationToken);
        return Ok(result);
    }

    [HttpGet("seat-heatmap")]
    public async Task<IActionResult> GetSeatHeatmap(
        [FromQuery] int roomId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int? movieId,
        [FromQuery] int? genreId,
        CancellationToken cancellationToken
    )
    {
        var result = await seatHeatmapService.GetAsync(
            roomId,
            from,
            to,
            movieId,
            genreId,
            cancellationToken
        );
        return Ok(result);
    }

    [HttpGet("schedule-report")]
    public async Task<IActionResult> GetScheduleReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken
    )
    {
        var result = await scheduleService.GetReportAsync(from, to, cancellationToken);
        return Ok(result);
    }

    [HttpGet("schedule-suggestions")]
    public async Task<IActionResult> GetScheduleSuggestions(CancellationToken cancellationToken)
    {
        var result = await suggestionService.GetAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("schedule-suggestions/apply")]
    public async Task<IActionResult> ApplyScheduleSuggestions(
        [FromBody] ApplyScheduleSuggestionsRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await suggestionApplyService.PreviewAsync(request, cancellationToken);
        return Ok(result);
    }
}
