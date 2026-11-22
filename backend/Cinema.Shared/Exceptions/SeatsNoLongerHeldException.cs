using Cinema.Shared.Resources;

namespace Cinema.Shared.Exceptions;

public class SeatsNoLongerHeldException(int orderId)
    : BusinessRuleException(string.Format(Messages.Rule_SeatsNoLongerHeld, orderId))
{
    public int OrderId { get; } = orderId;
}
