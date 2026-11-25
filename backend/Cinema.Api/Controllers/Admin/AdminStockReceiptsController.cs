using Cinema.Api.Filters;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces.Inventory;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/inventory/receipts")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminStockReceiptsController(IStockReceiptService stockReceiptService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int? inventoryItemId,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await stockReceiptService.GetAdminAsync(
                page,
                pageSize,
                new StockReceiptListFilter(search, fromDate, toDate, inventoryItemId),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await stockReceiptService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_StockReceiptCreated))]
    public async Task<IActionResult> Create(
        [FromBody] CreateStockReceiptRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await stockReceiptService.CreateAsync(request, cancellationToken)
        );
}
