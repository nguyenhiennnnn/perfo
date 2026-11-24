using Cinema.Api.Filters;
using Cinema.Application.DTOs.CounterShifts;
using Cinema.Application.Interfaces;
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
}
