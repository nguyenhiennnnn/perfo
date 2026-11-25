using Cinema.Application.Interfaces.Pricing;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/pricing/preview")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminPricingPreviewController(IPricingService pricingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Preview(
        [FromQuery] int showtimeId,
        [FromQuery] DateTime? at,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await pricingService.GetPriceTableAsync(
                showtimeId,
                at is null ? DateTime.UtcNow : BusinessTime.ToUtc(at.Value),
                cancellationToken
            )
        );
}
