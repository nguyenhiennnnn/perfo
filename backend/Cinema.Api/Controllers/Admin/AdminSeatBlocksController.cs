using Cinema.Api.Filters;
using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces.Bookings;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/showtimes/{showtimeId:int}/seat-blocks")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminSeatBlocksController(ISeatBlockService seatBlockService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByShowtime(
        int showtimeId,
        CancellationToken cancellationToken
    ) => Ok(await seatBlockService.GetByShowtimeAsync(showtimeId, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_SeatsBlocked))]
    public async Task<IActionResult> Block(
        int showtimeId,
        [FromBody] BlockSeatsRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await seatBlockService.BlockAsync(showtimeId, request, cancellationToken)
        );

    [HttpPost("unblock")]
    [SuccessMessage(nameof(Messages.Success_SeatsUnblocked))]
    public async Task<IActionResult> Unblock(
        int showtimeId,
        [FromBody] UnblockSeatsRequest request,
        CancellationToken cancellationToken
    ) => Ok(await seatBlockService.UnblockAsync(showtimeId, request, cancellationToken));
}
