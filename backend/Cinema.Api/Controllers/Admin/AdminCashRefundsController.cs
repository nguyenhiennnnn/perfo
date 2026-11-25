using Cinema.Api.Filters;
using Cinema.Application.Interfaces.Orders;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/cash-refunds")]
[Authorize(Policy = Policies.StaffOnly)]
public class AdminCashRefundsController(ICashRefundService cashRefundService) : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCashRefunds(
        [FromQuery] CashRefundListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await cashRefundService.GetCashRefundsAsync(filter, page, pageSize, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [SuccessMessage(nameof(Messages.Success_CashRefundConfirmed))]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken) =>
        Ok(await cashRefundService.ConfirmAsync(id, CurrentUserId, cancellationToken));
}
