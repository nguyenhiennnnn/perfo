using Cinema.Api.Filters;
using Cinema.Application.DTOs.Waitlist;
using Cinema.Application.Interfaces.Waitlist;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/waitlist")]
[Authorize(Policy = Policies.CustomerOnly)]
public class WaitlistController(IWaitlistService waitlistService) : AppControllerBase
{
    [HttpGet("showtimes/{showtimeId:int}")]
    public async Task<ActionResult<WaitlistEntryDto>> GetMine(
        int showtimeId,
        CancellationToken cancellationToken
    ) => Ok(await waitlistService.GetMineAsync(CurrentUserId, showtimeId, cancellationToken));

    [HttpPost("showtimes/{showtimeId:int}")]
    [SuccessMessage(nameof(Messages.Success_WaitlistRegistered))]
    public async Task<IActionResult> Register(
        int showtimeId,
        [FromBody] RegisterWaitlistRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await waitlistService.RegisterAsync(
            CurrentUserId,
            showtimeId,
            request,
            cancellationToken
        );
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("showtimes/{showtimeId:int}")]
    [SuccessMessage(nameof(Messages.Success_WaitlistWithdrawn))]
    public async Task<ActionResult<WaitlistEntryDto>> Withdraw(
        int showtimeId,
        CancellationToken cancellationToken
    ) => Ok(await waitlistService.WithdrawAsync(CurrentUserId, showtimeId, cancellationToken));
}
