using Cinema.Domain.Enums;

namespace Cinema.Application.Utils;

public static class CardCheckRules
{
    public static CardCheckStatus InitialStatus(bool requiresCardPresentation) =>
        requiresCardPresentation ? CardCheckStatus.Pending : CardCheckStatus.NotRequired;
}
