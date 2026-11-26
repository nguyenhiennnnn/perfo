using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/room-rental-quotes")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminRoomRentalQuotesController(IRoomRentalQuoteService roomRentalQuoteService)
    : ControllerBase
{
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(
        [FromBody] RoomRentalQuoteRequest request,
        CancellationToken cancellationToken
    ) => Ok(await roomRentalQuoteService.CalculateAsync(request, cancellationToken));
}
