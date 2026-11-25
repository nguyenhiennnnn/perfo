using Cinema.Api.Filters;
using Cinema.Application.DTOs.Rooms;
using Cinema.Application.Interfaces.Rooms;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/room-closures")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminRoomClosuresController(IRoomClosureService roomClosureService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] RoomClosureListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await roomClosureService.GetAdminAsync(filter, page, pageSize, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await roomClosureService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_RoomClosureCreated))]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoomClosureRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await roomClosureService.CreateAsync(request, cancellationToken)
        );

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_RoomClosureDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await roomClosureService.DeleteAsync(id, cancellationToken));

    [HttpGet("{id:int}/affected-showtimes")]
    public async Task<IActionResult> GetClosureAffectedShowtimes(
        int id,
        CancellationToken cancellationToken
    ) => Ok(await roomClosureService.GetClosureAffectedShowtimesAsync(id, cancellationToken));

    [HttpGet("affected-showtimes")]
    public async Task<IActionResult> GetAffectedShowtimes(
        [FromQuery] int roomId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await roomClosureService.GetAffectedShowtimesAsync(
                roomId,
                startTime,
                endTime,
                cancellationToken
            )
        );
}
