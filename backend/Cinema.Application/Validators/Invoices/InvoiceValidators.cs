using System.Text.RegularExpressions;
using Cinema.Application.DTOs.Invoices;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Invoices;

public static partial class InvoiceRules
{
    [GeneratedRegex(@"^\d{10}(-\d{3})?$")]
    private static partial Regex TaxCodePattern();

    public static bool IsValidTaxCode(string? value) =>
        !string.IsNullOrWhiteSpace(value) && TaxCodePattern().IsMatch(value.Trim());

    public static IRuleBuilderOptions<T, string> ValidTaxCode<T>(
        this IRuleBuilder<T, string> ruleBuilder
    ) =>
        ruleBuilder
            .NotEmpty()
            .MaximumLength(FieldLengths.TaxCode)
            .Must(IsValidTaxCode)
            .WithMessage(Messages.Validation_Invoice_TaxCode);

    public static IRuleBuilderOptions<T, string> ValidCompanyName<T>(
        this IRuleBuilder<T, string> ruleBuilder
    ) =>
        ruleBuilder
            .NotEmpty()
            .MaximumLength(FieldLengths.InvoiceCompanyName)
            .NoControlCharacters();

    public static IRuleBuilderOptions<T, string> ValidAddress<T>(
        this IRuleBuilder<T, string> ruleBuilder
    ) =>
        ruleBuilder
            .NotEmpty()
            .MaximumLength(FieldLengths.InvoiceAddress)
            .NoControlCharacters();

    public static IRuleBuilderOptions<T, string> ValidInvoiceEmail<T>(
        this IRuleBuilder<T, string> ruleBuilder
    ) => ruleBuilder.NotEmpty().EmailAddress().MaximumLength(FieldLengths.Email);
}

public class UpsertInvoiceProfileRequestValidator : AbstractValidator<UpsertInvoiceProfileRequest>
{
    public UpsertInvoiceProfileRequestValidator()
    {
        RuleFor(x => x.TaxCode).ValidTaxCode();
        RuleFor(x => x.CompanyName).ValidCompanyName();
        RuleFor(x => x.Address).ValidAddress();
        RuleFor(x => x.Email).ValidInvoiceEmail();
    }
}

public class CreateInvoiceRequestRequestValidator : AbstractValidator<CreateInvoiceRequestRequest>
{
    public CreateInvoiceRequestRequestValidator()
    {
        RuleFor(x => x.InvoiceProfileId).GreaterThan(0);
    }
}

public class CounterInvoiceRequestRequestValidator
    : AbstractValidator<CounterInvoiceRequestRequest>
{
    public CounterInvoiceRequestRequestValidator()
    {
        RuleFor(x => x.TaxCode).ValidTaxCode();
        RuleFor(x => x.CompanyName).ValidCompanyName();
        RuleFor(x => x.Address).ValidAddress();
        RuleFor(x => x.Email).ValidInvoiceEmail();
    }
}

public class RejectInvoiceRequestValidator : AbstractValidator<RejectInvoiceRequest>
{
    public RejectInvoiceRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(FieldLengths.InvoiceRejectReason)
            .NoControlCharacters();
    }
}

public class ResolveInvoiceAdjustmentRequestValidator
    : AbstractValidator<ResolveInvoiceAdjustmentRequest>
{
    public ResolveInvoiceAdjustmentRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(FieldLengths.InvoiceRejectReason)
            .NoControlCharacters()
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}
