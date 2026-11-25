using Cinema.Application.Events;
using Cinema.Application.Interfaces.Events;
using Cinema.Application.Interfaces.Loyalty;

namespace Cinema.Application.Services.Loyalty;

public class ReferralRewardHandler(IReferralRewardService rewards)
    : IBackgroundEventHandler<OrderConfirmed>
{
    public Task HandleAsync(OrderConfirmed businessEvent, CancellationToken cancellationToken) =>
        rewards.RewardForOrderAsync(businessEvent.OrderId, cancellationToken);
}
