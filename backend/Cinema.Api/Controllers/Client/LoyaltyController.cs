using Cinema.Application.Interfaces.Loyalty;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/loyalty")]
[Authorize]
public class LoyaltyController(ILoyaltyMemberQueryService queryService) : AppControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken) =>
        Ok(await queryService.GetSummaryAsync(CurrentUserId, cancellationToken));

    [HttpGet("lots")]
    public async Task<IActionResult> GetLots(
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await queryService.GetLotsAsync(CurrentUserId, page, pageSize, cancellationToken));
}
