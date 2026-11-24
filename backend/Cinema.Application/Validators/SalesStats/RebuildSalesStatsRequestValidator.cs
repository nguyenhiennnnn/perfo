using Cinema.Application.DTOs.SalesStats;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.SalesStats;

public class RebuildSalesStatsRequestValidator : AbstractValidator<RebuildSalesStatsRequest>
{
    public RebuildSalesStatsRequestValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEqual(default(DateOnly))
            .WithMessage(Messages.Validation_SalesStatsRebuild_FromDate_Required);

        RuleFor(x => x.ToDate)
            .NotEqual(default(DateOnly))
            .WithMessage(Messages.Validation_SalesStatsRebuild_ToDate_Required);
    }
}
