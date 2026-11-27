using Cinema.Api.Filters;
using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces.Showtimes;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/showtimes/{showtimeId:int}/event")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminShowtimeEventsController(
    IShowtimeEventService showtimeEventService,
    IShowtimeEventLimitService showtimeEventLimitService,
    IShowtimeEventGiftService showtimeEventGiftService,
    IShowtimeEarlyAccessService showtimeEarlyAccessService
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int showtimeId, CancellationToken cancellationToken) =>
        Ok(await showtimeEventService.GetAsync(showtimeId, cancellationToken));

    [HttpPut]
    [SuccessMessage(nameof(Messages.Success_ShowtimeEventSaved))]
    public async Task<IActionResult> Upsert(
        int showtimeId,
        [FromBody] UpsertShowtimeEventRequest request,
        CancellationToken cancellationToken
    ) => Ok(await showtimeEventService.UpsertAsync(showtimeId, request, cancellationToken));

    [HttpGet("limits")]
    public async Task<IActionResult> GetLimits(
        int showtimeId,
        CancellationToken cancellationToken
    ) => Ok(await showtimeEventLimitService.GetAsync(showtimeId, cancellationToken));

    [HttpPut("limits")]
    [SuccessMessage(nameof(Messages.Success_ShowtimeEventLimitsSaved))]
    public async Task<IActionResult> UpsertLimits(
        int showtimeId,
        [FromBody] UpsertShowtimeEventLimitsRequest request,
        CancellationToken cancellationToken
    ) => Ok(await showtimeEventLimitService.UpsertAsync(showtimeId, request, cancellationToken));

    [HttpGet("gifts")]
    public async Task<IActionResult> GetGifts(int showtimeId, CancellationToken cancellationToken) =>
        Ok(await showtimeEventGiftService.GetAsync(showtimeId, cancellationToken));

    [HttpPut("gifts")]
    [SuccessMessage(nameof(Messages.Success_ShowtimeEventGiftsSaved))]
    public async Task<IActionResult> UpsertGifts(
        int showtimeId,
        [FromBody] UpsertShowtimeEventGiftsRequest request,
        CancellationToken cancellationToken
    ) => Ok(await showtimeEventGiftService.UpsertAsync(showtimeId, request, cancellationToken));

    [HttpGet("early-access")]
    public async Task<IActionResult> GetEarlyAccess(
        int showtimeId,
        CancellationToken cancellationToken
    ) => Ok(await showtimeEarlyAccessService.GetAsync(showtimeId, cancellationToken));

    [HttpPut("early-access")]
    [SuccessMessage(nameof(Messages.Success_ShowtimeEventEarlyAccessSaved))]
    public async Task<IActionResult> UpsertEarlyAccess(
        int showtimeId,
        [FromBody] UpsertShowtimeEarlyAccessRequest request,
        CancellationToken cancellationToken
    ) => Ok(await showtimeEarlyAccessService.UpsertAsync(showtimeId, request, cancellationToken));

    [HttpDelete("early-access")]
    [SuccessMessage(nameof(Messages.Success_ShowtimeEventEarlyAccessRemoved))]
    public async Task<IActionResult> RemoveEarlyAccess(
        int showtimeId,
        CancellationToken cancellationToken
    ) => Ok(await showtimeEarlyAccessService.RemoveAsync(showtimeId, cancellationToken));

    [HttpDelete]
    [SuccessMessage(nameof(Messages.Success_ShowtimeEventRemoved))]
    public async Task<IActionResult> Remove(int showtimeId, CancellationToken cancellationToken) =>
        Ok(await showtimeEventService.RemoveAsync(showtimeId, cancellationToken));
}
