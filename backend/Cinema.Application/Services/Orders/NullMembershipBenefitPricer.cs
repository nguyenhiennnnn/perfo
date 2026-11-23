using Cinema.Application.Interfaces;

namespace Cinema.Application.Services.Orders;

public class NullMembershipBenefitPricer : IMembershipBenefitPricer
{
    public Task<IReadOnlyList<MembershipBenefitDiscount>> ComputeAsync(
        MembershipBenefitContext context,
        CancellationToken cancellationToken
    ) => Task.FromResult<IReadOnlyList<MembershipBenefitDiscount>>([]);
}
