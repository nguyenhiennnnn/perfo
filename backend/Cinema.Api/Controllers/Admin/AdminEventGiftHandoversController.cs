using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Interfaces.Showtimes;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/event-gift-handovers")]
[Authorize(Policy = Policies.StaffOnly)]
public class AdminEventGiftHandoversController(
    IShowtimeEventGiftHandoverService handoverService
) : AppControllerBase
{
    [HttpGet("lookup")]
    public async Task<ActionResult<EventGiftHandoverTicketDto>> Lookup(
        [FromQuery] string qrCode,
        CancellationToken cancellationToken
    ) => Ok(await handoverService.LookupAsync(qrCode, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<EventGiftHandoverResponse>> HandOver(
        [FromBody] EventGiftHandoverRequest request,
        CancellationToken cancellationToken
    ) => Ok(await handoverService.HandOverAsync(CurrentUserId, request, cancellationToken));
}
