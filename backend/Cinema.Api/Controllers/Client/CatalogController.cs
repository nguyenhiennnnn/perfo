using Cinema.Application.Interfaces.Catalog;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix)]
public class CatalogController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet("genres")]
    public async Task<IActionResult> GetGenres(CancellationToken cancellationToken) =>
        Ok(await catalogService.GetGenresAsync(cancellationToken));

    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguages(CancellationToken cancellationToken) =>
        Ok(await catalogService.GetLanguagesAsync(cancellationToken));

    [HttpGet("age-ratings")]
    public async Task<IActionResult> GetAgeRatings(CancellationToken cancellationToken) =>
        Ok(await catalogService.GetAgeRatingsAsync(cancellationToken));

    [HttpGet("screen-types")]
    public async Task<IActionResult> GetScreenTypes(CancellationToken cancellationToken) =>
        Ok(
            await catalogService.GetScreenTypesAsync(
                Pagination.DefaultPage,
                Pagination.MaxPageSize,
                cancellationToken
            )
        );

    [HttpGet("membership-tiers")]
    public async Task<IActionResult> GetMembershipTiers(CancellationToken cancellationToken) =>
        Ok(await catalogService.GetMembershipTiersAsync(cancellationToken));
}
