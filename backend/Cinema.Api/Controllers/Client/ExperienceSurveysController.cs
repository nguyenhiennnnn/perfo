using Cinema.Api.Filters;
using Cinema.Application.DTOs.Surveys;
using Cinema.Application.Interfaces.Surveys;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/experience-surveys")]
[Authorize(Policy = Policies.CustomerOnly)]
public class ExperienceSurveysController(IExperienceSurveyService surveyService)
    : AppControllerBase
{
    [HttpGet("showtimes/{showtimeId:int}")]
    public async Task<ActionResult<ExperienceSurveyDto>> GetMine(
        int showtimeId,
        CancellationToken cancellationToken
    ) => Ok(await surveyService.GetMineAsync(CurrentUserId, showtimeId, cancellationToken));

    [HttpPost("showtimes/{showtimeId:int}")]
    [SuccessMessage(nameof(Messages.Success_ExperienceSurveySubmitted))]
    public async Task<IActionResult> Submit(
        int showtimeId,
        [FromBody] SubmitExperienceSurveyRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await surveyService.SubmitAsync(
            CurrentUserId,
            showtimeId,
            request,
            cancellationToken
        );
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
