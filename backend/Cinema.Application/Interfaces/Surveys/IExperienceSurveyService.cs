using Cinema.Application.DTOs.Surveys;

namespace Cinema.Application.Interfaces.Surveys;

public interface IExperienceSurveyService
{
    Task<ExperienceSurveyDto> SubmitAsync(
        int userId,
        int showtimeId,
        SubmitExperienceSurveyRequest request,
        CancellationToken cancellationToken
    );

    Task<ExperienceSurveyDto> GetMineAsync(
        int userId,
        int showtimeId,
        CancellationToken cancellationToken
    );
}
