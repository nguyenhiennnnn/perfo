using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Services.RoomRentals;
using Cinema.Application.Utils;
using Cinema.Application.Validators.Common;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using FluentValidation;

namespace Cinema.Application.Validators.RoomRentals;

public class CreateRoomRentalRequestRequestValidator
    : AbstractValidator<CreateRoomRentalRequestRequest>
{
    public CreateRoomRentalRequestRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(FieldLengths.FullName)
            .NoControlCharacters();
        RuleFor(x => x.OrganizationName!)
            .MaximumLength(FieldLengths.RoomRentalOrganization)
            .NoControlCharacters()
            .When(x => !string.IsNullOrEmpty(x.OrganizationName));
        RuleFor(x => x.AudienceType).IsInEnum();
        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .Matches(PhoneRules.Pattern)
            .WithMessage(Messages.Validation_Phone_Invalid)
            .MaximumLength(FieldLengths.Phone);
        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(FieldLengths.Email);
        RuleFor(x => x.DesiredStartAt)
            .Must(value => BusinessTime.ToUtc(value) > DateTime.UtcNow)
            .WithMessage(Messages.Validation_RoomRental_DesiredStartPast);
        RuleFor(x => x)
            .Must(x => x.FlexibleFromAt.HasValue == x.FlexibleToAt.HasValue)
            .WithMessage(Messages.Validation_RoomRental_FlexibleRangeIncomplete)
            .OverridePropertyName(nameof(CreateRoomRentalRequestRequest.FlexibleFromAt));
        RuleFor(x => x)
            .Must(x =>
                x.FlexibleFromAt is not { } start
                || x.FlexibleToAt is not { } end
                || (
                    start <= end
                    && (end - start).TotalDays <= RoomRentalLimits.MaxFlexibleSpanDays
                )
            )
            .WithMessage(
                string.Format(
                    Messages.Validation_RoomRental_FlexibleRangeInvalid,
                    RoomRentalLimits.MaxFlexibleSpanDays
                )
            )
            .OverridePropertyName(nameof(CreateRoomRentalRequestRequest.FlexibleToAt));
        RuleFor(x => x.AttendeeCount)
            .InclusiveBetween(RoomRentalLimits.MinAttendees, RoomRentalLimits.MaxAttendees)
            .WithMessage(
                string.Format(
                    Messages.Validation_RoomRental_AttendeeCountRange,
                    RoomRentalLimits.MinAttendees,
                    RoomRentalLimits.MaxAttendees
                )
            );
        RuleFor(x => x.MovieId).GreaterThan(0).When(x => x.MovieId is not null);
        RuleFor(x => x.MovieId)
            .Null()
            .When(x => x.BringsOwnContent)
            .WithMessage(Messages.Validation_RoomRental_ContentConflict);
        RuleFor(x => x.ContentNote!)
            .MaximumLength(FieldLengths.RoomRentalNote)
            .NoControlCharacters()
            .When(x => !string.IsNullOrEmpty(x.ContentNote));
        RuleFor(x => x.ScreenTypeId).GreaterThan(0).When(x => x.ScreenTypeId is not null);
        RuleFor(x => x.ComboRequest!)
            .MaximumLength(FieldLengths.RoomRentalNote)
            .NoControlCharacters()
            .When(x => !string.IsNullOrEmpty(x.ComboRequest));
        RuleFor(x => x.Notes!)
            .MaximumLength(FieldLengths.RoomRentalNote)
            .NoControlCharacters()
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
