using Cinema.Api.Filters;
using Cinema.Application.Interfaces.Campaigns;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/campaign-tracking")]
[AllowAnonymous]
public class CampaignTrackingController(ICampaignOpenTracker openTracker) : AppControllerBase
{
    private static readonly byte[] Pixel = Convert.FromBase64String(
        "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"
    );

    [HttpGet("{token}")]
    [RawResponse]
    public async Task<IActionResult> Open(string token, CancellationToken cancellationToken)
    {
        await openTracker.RecordAsync(token, cancellationToken);
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        Response.Headers.Pragma = "no-cache";
        return File(Pixel, "image/gif");
    }
}
