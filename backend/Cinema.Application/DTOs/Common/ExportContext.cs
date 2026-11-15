using Cinema.Application.Utils;

namespace Cinema.Application.DTOs.Common;

public sealed record ExportContext(ActorContext Actor)
{
    public bool MaskPersonalData => !Actor.IsAdmin;

    public string? Phone(string? value) =>
        MaskPersonalData ? PersonalDataMasker.MaskPhone(value) : value;

    public string? Email(string? value) =>
        MaskPersonalData ? PersonalDataMasker.MaskEmail(value) : value;
}
