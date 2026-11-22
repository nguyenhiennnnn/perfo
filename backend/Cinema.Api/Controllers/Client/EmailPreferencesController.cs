using Cinema.Api.Extensions;
using Cinema.Application.DTOs.Users;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/email-preferences")]
[AllowAnonymous]
public class EmailPreferencesController(ICommunicationPreferenceService preferenceService)
    : AppControllerBase
{
    [HttpPost("unsubscribe")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> Unsubscribe(
        [FromBody] UnsubscribeRequest request,
        CancellationToken cancellationToken
    ) => Ok(await preferenceService.UnsubscribeMarketingAsync(request.Token, cancellationToken));
}
