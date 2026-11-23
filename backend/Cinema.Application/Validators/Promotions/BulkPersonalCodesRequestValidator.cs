using System.Text.RegularExpressions;
using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Services.Promotions;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Promotions;

public partial class BulkPersonalCodesRequestValidator : AbstractValidator<BulkPersonalCodesRequest>
{
    private static readonly UpsertPromotionRequestValidator TemplateValidator = new();

    private static readonly HashSet<string> OwnedByBulk =
    [
        "Code",
        "Type",
        "AssignedUserId",
        "TotalUsageLimit",
        "PerCustomerLimit",
        "IsActive",
    ];

    public BulkPersonalCodesRequestValidator()
    {
        RuleFor(x => x.CodePrefix)
            .NotEmpty()
            .Must(prefix => PrefixRegex().IsMatch(prefix.Trim()))
            .When(x => !string.IsNullOrWhiteSpace(x.CodePrefix))
            .WithMessage(
                string.Format(
                    Messages.Validation_Promotion_BulkPrefixFormat,
                    PromotionLimits.MaxCodePrefixLength
                )
            );

        RuleFor(x => x.UserIds)
            .NotEmpty()
            .WithMessage(Messages.Validation_Promotion_BulkUsersRequired)
            .Must(ids => ids.Count <= PromotionLimits.MaxBulkPersonalCodes && ids.All(id => id > 0))
            .When(x => x.UserIds is { Count: > 0 })
            .WithMessage(
                string.Format(
                    Messages.Validation_Promotion_BulkUsersRange,
                    PromotionLimits.MaxBulkPersonalCodes
                )
            );

        RuleFor(x => x.ValidDays)
            .InclusiveBetween(1, PromotionLimits.MaxPersonalCodeValidDays)
            .When(x => x.ValidDays is not null)
            .WithMessage(
                string.Format(
                    Messages.Validation_Promotion_BulkValidDaysRange,
                    PromotionLimits.MaxPersonalCodeValidDays
                )
            );

        RuleFor(x => x)
            .Custom(
                (request, context) =>
                {
                    var sample = PromotionBulkMapper.ToUpsert(
                        request,
                        "BULK-SAMPLE",
                        request.UserIds?.FirstOrDefault(id => id > 0) ?? 1,
                        request.EndsAt
                    );
                    foreach (
                        var failure in TemplateValidator
                            .Validate(sample)
                            .Errors.Where(e => !OwnedByBulk.Contains(e.PropertyName))
                    )
                        context.AddFailure(ToCamelCase(failure.PropertyName), failure.ErrorMessage);
                }
            );
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    [GeneratedRegex(
        "^[A-Za-z0-9]{2,12}$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex PrefixRegex();
}
