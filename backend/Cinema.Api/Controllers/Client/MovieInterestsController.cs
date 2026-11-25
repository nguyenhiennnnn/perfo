using Cinema.Api.Filters;
using Cinema.Application.DTOs.Movies;
using Cinema.Application.Interfaces.Movies;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/movie-interests")]
[Authorize(Policy = Policies.CustomerOnly)]
public class MovieInterestsController(IMovieInterestService interestService) : AppControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MovieInterestDto>>> GetMine(
        CancellationToken cancellationToken
    ) => Ok(await interestService.GetMineAsync(CurrentUserId, cancellationToken));

    [HttpPost("movies/{movieId:int}")]
    [SuccessMessage(nameof(Messages.Success_MovieInterestAdded))]
    public async Task<IActionResult> Add(int movieId, CancellationToken cancellationToken)
    {
        var result = await interestService.AddAsync(CurrentUserId, movieId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("movies/{movieId:int}")]
    [SuccessMessage(nameof(Messages.Success_MovieInterestRemoved))]
    public async Task<ActionResult<RemoveMovieInterestResponse>> Remove(
        int movieId,
        CancellationToken cancellationToken
    ) => Ok(await interestService.RemoveAsync(CurrentUserId, movieId, cancellationToken));
}
