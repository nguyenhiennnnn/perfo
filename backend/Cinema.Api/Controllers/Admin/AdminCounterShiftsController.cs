using Cinema.Api.Filters;
using Cinema.Application.DTOs.CounterShifts;
using Cinema.Application.Interfaces.CounterShifts;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/counter-shifts")]
[Authorize(Policy = Policies.StaffOnly)]
public class AdminCounterShiftsController(ICounterShiftService counterShiftService)
    : AppControllerBase
{
    private CounterShiftActor Actor =>
        new(CurrentUserId, CurrentEmployeeIdOrNull, CurrentActor.IsAdmin);

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] CounterShiftListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await counterShiftService.GetShiftsAsync(Actor, filter, page, pageSize, cancellationToken));

    [HttpGet("open")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetOpen(CancellationToken cancellationToken) =>
        Ok(await counterShiftService.GetOpenShiftsAsync(cancellationToken));

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken) =>
        Ok(await counterShiftService.GetCurrentAsync(Actor, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await counterShiftService.GetByIdAsync(Actor, id, cancellationToken));

    [HttpPost("open")]
    [SuccessMessage(nameof(Messages.Success_CounterShiftOpened))]
    public async Task<IActionResult> Open(
        [FromBody] OpenCounterShiftRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await counterShiftService.OpenAsync(Actor, request, cancellationToken)
        );

    [HttpPost("{id:int}/close")]
    [SuccessMessage(nameof(Messages.Success_CounterShiftClosed))]
    public async Task<IActionResult> Close(
        int id,
        [FromBody] CloseCounterShiftRequest request,
        CancellationToken cancellationToken
    ) => Ok(await counterShiftService.CloseAsync(Actor, id, request, cancellationToken));

    [HttpPost("{id:int}/close-by-admin")]
    [Authorize(Policy = Policies.AdminOnly)]
    [SuccessMessage(nameof(Messages.Success_CounterShiftClosedByAdmin))]
    public async Task<IActionResult> CloseByAdmin(
        int id,
        [FromBody] AdminCloseCounterShiftRequest request,
        CancellationToken cancellationToken
    ) => Ok(await counterShiftService.CloseByAdminAsync(Actor, id, request, cancellationToken));
}
