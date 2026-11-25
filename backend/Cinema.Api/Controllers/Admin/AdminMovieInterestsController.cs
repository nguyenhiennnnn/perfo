using Cinema.Application.Interfaces.Movies;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/movie-interests")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminMovieInterestsController(IMovieInterestService interestService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCounts(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await interestService.GetCountsAdminAsync(
                status,
                search,
                page,
                pageSize,
                cancellationToken
            )
        );
}
