using Cinema.Api.Filters;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/inventory/waste")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminWasteController(IWasteService wasteService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? reason,
        [FromQuery] int? inventoryItemId,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await wasteService.GetAdminAsync(
                page,
                pageSize,
                new WasteSheetListFilter(search, fromDate, toDate, reason, inventoryItemId),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await wasteService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_WasteSheetCreated))]
    public async Task<IActionResult> Create(
        [FromBody] CreateWasteSheetRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await wasteService.CreateAsync(request, cancellationToken)
        );
}
