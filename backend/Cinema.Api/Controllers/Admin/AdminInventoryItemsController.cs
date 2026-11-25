using Cinema.Api.Filters;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces.Inventory;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/inventory/items")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminInventoryItemsController(IInventoryItemService inventoryItemService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? lowStockOnly,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await inventoryItemService.GetAdminAsync(
                page,
                pageSize,
                new InventoryItemListFilter(search, isActive, lowStockOnly),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await inventoryItemService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_InventoryItemCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertInventoryItemRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await inventoryItemService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_InventoryItemUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertInventoryItemRequest request,
        CancellationToken cancellationToken
    ) => Ok(await inventoryItemService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_InventoryItemDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await inventoryItemService.DeleteAsync(id, cancellationToken));
}
