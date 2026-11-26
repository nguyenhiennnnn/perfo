using Cinema.Application.DTOs.Bundles;
using Cinema.Application.Services.Bundles;

namespace Cinema.Application.Interfaces.Bundles;

public interface IBundleRetailPricer
{
    Task<BundleRetailQuote?> QuoteAsync(
        BundleRetailInput input,
        CancellationToken cancellationToken
    );
}
