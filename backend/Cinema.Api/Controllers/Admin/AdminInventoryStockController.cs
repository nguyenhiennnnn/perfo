using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/inventory")]
[Authorize]
public class AdminInventoryStockController(IInventoryStockQueryService stockQueryService)
    : ControllerBase
{
    [HttpGet("stock")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<IActionResult> GetStockLevels(
        [FromQuery] string? search,
        [FromQuery] bool? lowStockOnly,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await stockQueryService.GetStockLevelsAsync(
                page,
                pageSize,
                new StockLevelFilter(search, lowStockOnly),
                cancellationToken
            )
        );

    [HttpGet("alerts")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<IActionResult> GetAlerts(CancellationToken cancellationToken) =>
        Ok(await stockQueryService.GetAlertsAsync(cancellationToken));

    [HttpGet("ledger")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetLedger(
        [FromQuery] int inventoryItemId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await stockQueryService.GetLedgerAsync(
                inventoryItemId,
                fromDate,
                toDate,
                page,
                pageSize,
                cancellationToken
            )
        );

    [HttpGet("valuation")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetValuation(CancellationToken cancellationToken) =>
        Ok(await stockQueryService.GetValuationAsync(cancellationToken));
}
