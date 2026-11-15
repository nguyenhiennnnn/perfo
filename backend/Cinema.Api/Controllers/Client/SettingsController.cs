using Cinema.Application.DTOs.Settings;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/settings")]
public class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CinemaSettingsDto>> GetSettings(
        CancellationToken cancellationToken
    ) => Ok(await settingsService.GetSettingsAsync(cancellationToken));
}
