using Cinema.Application.DTOs.Surveys;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Surveys;

public class SubmitExperienceSurveyRequestValidator : AbstractValidator<SubmitExperienceSurveyRequest>
{
    public SubmitExperienceSurveyRequestValidator()
    {
        RuleFor(x => x.RoomRating).InclusiveBetween(1, 5).WithMessage(Messages.Validation_Rating_Range1To5);
        RuleFor(x => x.SoundRating).InclusiveBetween(1, 5).WithMessage(Messages.Validation_Rating_Range1To5);
        RuleFor(x => x.PictureRating).InclusiveBetween(1, 5).WithMessage(Messages.Validation_Rating_Range1To5);
        RuleFor(x => x.CleanlinessRating)
            .InclusiveBetween(1, 5)
            .WithMessage(Messages.Validation_Rating_Range1To5);
        RuleFor(x => x.CounterServiceRating)
            .InclusiveBetween(1, 5)
            .WithMessage(Messages.Validation_Rating_Range1To5);
    }
}
