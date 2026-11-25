using Cinema.Application.Services.Orders;

namespace Cinema.Application.Interfaces.Pricing;

public interface IMembershipBenefitPricer
{
    Task<IReadOnlyList<MembershipBenefitDiscount>> ComputeAsync(
        MembershipBenefitContext context,
        CancellationToken cancellationToken
    );
}
