using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/combo-pickups")]
[Authorize(Policy = Policies.StaffOnly)]
public class AdminComboPickupController(
    IComboPickupService comboPickupService,
    IUnclaimedComboOrderService unclaimedComboOrderService
) : AppControllerBase
{
    [HttpGet("lookup")]
    public async Task<ActionResult<ComboPickupOrderDto>> Lookup(
        [FromQuery] string code,
        CancellationToken cancellationToken
    ) => Ok(await comboPickupService.LookupAsync(code, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ComboPickupResponse>> PickUp(
        [FromBody] ComboPickupRequest request,
        CancellationToken cancellationToken
    ) => Ok(await comboPickupService.PickUpAsync(CurrentUserId, request, cancellationToken));

    [HttpGet("unclaimed")]
    public async Task<IActionResult> GetUnclaimed(
        [FromQuery] string? search,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await unclaimedComboOrderService.GetAsync(page, pageSize, search, cancellationToken));
}
