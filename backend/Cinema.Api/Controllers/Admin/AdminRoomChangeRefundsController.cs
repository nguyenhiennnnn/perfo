using Cinema.Api.Filters;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/room-change-refunds")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminRoomChangeRefundsController(IRoomChangeRefundService roomChangeRefundService)
    : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRefundDues(
        [FromQuery] RoomChangeRefundListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(await roomChangeRefundService.GetRefundDuesAsync(filter, page, pageSize, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [SuccessMessage(nameof(Messages.Success_RoomChangeRefundConfirmed))]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken) =>
        Ok(await roomChangeRefundService.ConfirmAsync(id, CurrentUserId, cancellationToken));
}
