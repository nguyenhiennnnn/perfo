using System.Security.Claims;
using Cinema.Application.Interfaces.Bundles;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/bundles")]
public class BundlesController(
    IBundleAvailabilityService bundleAvailabilityService,
    IBundleViewTracker bundleViewTracker
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetForShowtime(
        [FromQuery] int showtimeId,
        CancellationToken cancellationToken
    )
    {
        if (showtimeId < 1)
            throw new ValidationAppException(
                Messages.Invalid_BundleShowtimeIdRequired,
                new Dictionary<string, string[]>
                {
                    ["showtimeId"] = [Messages.Invalid_BundleShowtimeIdRequired],
                }
            );

        var bundles = await bundleAvailabilityService.GetForShowtimeAsync(
            showtimeId,
            cancellationToken
        );
        if (
            bundles.Count > 0
            && int.TryParse(User.FindFirstValue(AppClaimTypes.UserId), out var userId)
        )
            await bundleViewTracker.RecordAsync(
                userId,
                showtimeId,
                [.. bundles.Select(b => b.BundleId)],
                cancellationToken
            );

        return Ok(bundles);
    }
}
