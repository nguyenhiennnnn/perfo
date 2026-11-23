using System.Text.RegularExpressions;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Services.Inventory;
using Cinema.Application.Utils;
using Cinema.Application.Validators.Common;
using Cinema.Application.Validators.Pricing;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Inventory;

public static class InventoryValidation
{
    public static bool IsValidQuantity(decimal value) =>
        value > 0
        && value <= InventoryLimits.MaxQuantity
        && decimal.Round(value, InventoryLimits.QuantityDecimals) == value;

    public static bool IsValidCountedQuantity(decimal value) =>
        value >= 0
        && value <= InventoryLimits.MaxQuantity
        && decimal.Round(value, InventoryLimits.QuantityDecimals) == value;

    public static bool IsValidThreshold(decimal value) =>
        value >= 0
        && value <= InventoryLimits.MaxQuantity
        && decimal.Round(value, InventoryLimits.QuantityDecimals) == value;

    public static bool IsValidUnitCost(decimal value) =>
        value >= 0 && value <= InventoryLimits.MaxUnitCost && value == decimal.Truncate(value);
}

public partial class UpsertInventoryItemRequestValidator
    : AbstractValidator<UpsertInventoryItemRequest>
{
    public UpsertInventoryItemRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.Code)
            .Must(code => CodeRegex().IsMatch(code.Trim()))
            .When(x => !string.IsNullOrWhiteSpace(x.Code))
            .WithMessage(Messages.Validation_InventoryItem_CodeFormat);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(FieldLengths.InventoryItemName)
            .NoControlCharacters();

        RuleFor(x => x.Unit)
            .NotEmpty()
            .MaximumLength(FieldLengths.InventoryUnit)
            .NoControlCharacters();

        RuleFor(x => x.LowStockThreshold)
            .Must(InventoryValidation.IsValidThreshold)
            .WithMessage(
                string.Format(Messages.Validation_Inventory_ThresholdRange, InventoryLimits.MaxQuantity)
            );
    }

    [GeneratedRegex(
        "^[A-Za-z0-9_-]{2,30}$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex CodeRegex();
}

public class UpsertComboRecipeRequestValidator : AbstractValidator<UpsertComboRecipeRequest>
{
    public UpsertComboRecipeRequestValidator()
    {
        RuleFor(x => x.Lines)
            .NotNull()
            .Must(lines => lines is { Count: > 0 and <= InventoryLimits.MaxRecipeLines })
            .WithMessage(
                string.Format(Messages.Validation_ComboRecipe_LinesRange, InventoryLimits.MaxRecipeLines)
            );

        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.InventoryItemId).Distinct().Count() == lines.Count)
            .When(x => x.Lines is { Count: > 0 })
            .WithMessage(Messages.Validation_ComboRecipe_ItemDuplicated);

        RuleForEach(x => x.Lines).SetValidator(new ComboRecipeLineRequestValidator());
    }
}

public class ComboRecipeLineRequestValidator : AbstractValidator<ComboRecipeLineRequest>
{
    public ComboRecipeLineRequestValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_InventoryItemId_Positive);

        RuleFor(x => x.Quantity)
            .Must(InventoryValidation.IsValidQuantity)
            .WithMessage(
                string.Format(Messages.Validation_Inventory_QuantityRange, InventoryLimits.MaxQuantity)
            );
    }
}

public class CreateStockReceiptRequestValidator : AbstractValidator<CreateStockReceiptRequest>
{
    public CreateStockReceiptRequestValidator()
    {
        RuleFor(x => x.SupplierName)
            .NotEmpty()
            .MaximumLength(FieldLengths.SupplierName)
            .NoControlCharacters();

        RuleFor(x => x.ReceivedDate)
            .Must(date => date != default && date <= BusinessTime.Today())
            .WithMessage(Messages.Validation_StockReceipt_DateInvalid);

        RuleFor(x => x.Note).MaximumLength(FieldLengths.StockNote).When(x => x.Note is not null);

        RuleFor(x => x.Lines)
            .NotNull()
            .Must(lines => lines is { Count: > 0 and <= InventoryLimits.MaxReceiptLines })
            .WithMessage(
                string.Format(Messages.Validation_StockReceipt_LinesRange, InventoryLimits.MaxReceiptLines)
            );

        RuleForEach(x => x.Lines).SetValidator(new StockReceiptLineRequestValidator());
    }
}

public class StockReceiptLineRequestValidator : AbstractValidator<StockReceiptLineRequest>
{
    public StockReceiptLineRequestValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_InventoryItemId_Positive);

        RuleFor(x => x.Quantity)
            .Must(InventoryValidation.IsValidQuantity)
            .WithMessage(
                string.Format(Messages.Validation_Inventory_QuantityRange, InventoryLimits.MaxQuantity)
            );

        RuleFor(x => x.UnitCost)
            .Must(InventoryValidation.IsValidUnitCost)
            .WithMessage(
                string.Format(Messages.Validation_Inventory_UnitCostRange, InventoryLimits.MaxUnitCost)
            );
    }
}

public class CreateStocktakeSheetRequestValidator : AbstractValidator<CreateStocktakeSheetRequest>
{
    public CreateStocktakeSheetRequestValidator()
    {
        RuleFor(x => x.StocktakeDate)
            .Must(date => date != default && date <= BusinessTime.Today())
            .WithMessage(Messages.Validation_StocktakeSheet_DateInvalid);

        RuleFor(x => x.Note).MaximumLength(FieldLengths.StockNote).When(x => x.Note is not null);

        RuleFor(x => x.Lines)
            .NotNull()
            .Must(lines => lines is { Count: > 0 and <= InventoryLimits.MaxStocktakeLines })
            .WithMessage(
                string.Format(
                    Messages.Validation_StocktakeSheet_LinesRange,
                    InventoryLimits.MaxStocktakeLines
                )
            );

        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.InventoryItemId).Distinct().Count() == lines.Count)
            .When(x => x.Lines is { Count: > 0 })
            .WithMessage(Messages.Validation_StocktakeSheet_ItemDuplicated);

        RuleForEach(x => x.Lines).SetValidator(new StocktakeSheetLineRequestValidator());
    }
}

public class StocktakeSheetLineRequestValidator : AbstractValidator<StocktakeSheetLineRequest>
{
    public StocktakeSheetLineRequestValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_InventoryItemId_Positive);

        RuleFor(x => x.CountedQuantity)
            .Must(InventoryValidation.IsValidCountedQuantity)
            .WithMessage(
                string.Format(
                    Messages.Validation_Inventory_CountedQuantityRange,
                    InventoryLimits.MaxQuantity
                )
            );
    }
}

public class CreateWasteSheetRequestValidator : AbstractValidator<CreateWasteSheetRequest>
{
    public CreateWasteSheetRequestValidator()
    {
        RuleFor(x => x.WasteDate)
            .Must(date => date != default && date <= BusinessTime.Today())
            .WithMessage(Messages.Validation_WasteSheet_DateInvalid);

        RuleFor(x => x.Reason)
            .Must(v => PricingValidation.TryParseEnum<WasteReason>(v, out _))
            .WithMessage(Messages.Validation_WasteSheet_ReasonInvalid);

        RuleFor(x => x.Note).MaximumLength(FieldLengths.StockNote).When(x => x.Note is not null);

        RuleFor(x => x.Note)
            .Must(note => !string.IsNullOrWhiteSpace(note))
            .When(x => IsOtherReason(x.Reason))
            .WithMessage(Messages.Validation_WasteSheet_NoteRequiredForOther);

        RuleFor(x => x.Lines)
            .NotNull()
            .Must(lines => lines is { Count: > 0 and <= InventoryLimits.MaxWasteLines })
            .WithMessage(
                string.Format(Messages.Validation_WasteSheet_LinesRange, InventoryLimits.MaxWasteLines)
            );

        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.InventoryItemId).Distinct().Count() == lines.Count)
            .When(x => x.Lines is { Count: > 0 })
            .WithMessage(Messages.Validation_WasteSheet_ItemDuplicated);

        RuleForEach(x => x.Lines).SetValidator(new WasteSheetLineRequestValidator());
    }

    private static bool IsOtherReason(string? reason) =>
        PricingValidation.TryParseEnum<WasteReason>(reason, out var parsed)
        && parsed == WasteReason.Other;
}

public class WasteSheetLineRequestValidator : AbstractValidator<WasteSheetLineRequest>
{
    public WasteSheetLineRequestValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .GreaterThan(0)
            .WithMessage(Messages.Validation_InventoryItemId_Positive);

        RuleFor(x => x.Quantity)
            .Must(InventoryValidation.IsValidQuantity)
            .WithMessage(
                string.Format(Messages.Validation_Inventory_QuantityRange, InventoryLimits.MaxQuantity)
            );
    }
}
