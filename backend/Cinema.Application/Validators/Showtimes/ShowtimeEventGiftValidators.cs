using Cinema.Application.DTOs.Showtimes;
using Cinema.Application.Services.Showtimes;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.Showtimes;

public class UpsertShowtimeEventGiftsRequestValidator
    : AbstractValidator<UpsertShowtimeEventGiftsRequest>
{
    public UpsertShowtimeEventGiftsRequestValidator()
    {
        RuleFor(x => x.Gifts)
            .Must(g => g is null || g.Select(i => i.InventoryItemId).Distinct().Count() == g.Count)
            .WithMessage(Messages.Validation_EventGift_ItemDuplicate);

        RuleForEach(x => x.Gifts)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.InventoryItemId)
                    .GreaterThan(0)
                    .WithMessage(Messages.Validation_InventoryItemId_Positive);
                item.RuleFor(i => i.Quantity)
                    .InclusiveBetween(1, ShowtimeEventGiftRules.MaxQuantity)
                    .WithMessage(
                        string.Format(
                            Messages.Validation_EventGift_QuantityRange,
                            ShowtimeEventGiftRules.MaxQuantity
                        )
                    );
                item.RuleFor(i => i.QuantityPerTicket)
                    .InclusiveBetween(1, ShowtimeEventGiftRules.MaxPerTicket)
                    .WithMessage(
                        string.Format(
                            Messages.Validation_EventGift_PerTicketRange,
                            ShowtimeEventGiftRules.MaxPerTicket
                        )
                    );
            });
    }
}
