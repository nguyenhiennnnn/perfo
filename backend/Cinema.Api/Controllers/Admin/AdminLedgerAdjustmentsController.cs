using Cinema.Api.Filters;
using Cinema.Application.DTOs.Ledger;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/ledger-adjustments")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminLedgerAdjustmentsController(ILedgerAdjustmentService adjustmentService)
    : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] LedgerAdjustmentListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await adjustmentService.GetAdjustmentsAsync(filter, page, pageSize, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_LedgerAdjustmentCreated))]
    public async Task<IActionResult> Create(
        [FromBody] CreateLedgerAdjustmentRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await adjustmentService.CreateManualAsync(CurrentUserId, request, cancellationToken)
        );
}
