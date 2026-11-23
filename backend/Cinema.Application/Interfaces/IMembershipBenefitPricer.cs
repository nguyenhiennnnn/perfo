using Cinema.Application.Services.Orders;

namespace Cinema.Application.Interfaces;

public interface IMembershipBenefitPricer
{
    Task<IReadOnlyList<MembershipBenefitDiscount>> ComputeAsync(
        MembershipBenefitContext context,
        CancellationToken cancellationToken
    );
}
