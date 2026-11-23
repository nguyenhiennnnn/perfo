using Cinema.Api.Filters;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/inventory/stocktakes")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminStocktakesController(IStocktakeService stocktakeService) : ControllerBase
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
            await stocktakeService.GetAdminAsync(
                page,
                pageSize,
                new StocktakeSheetListFilter(search, fromDate, toDate, inventoryItemId),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await stocktakeService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_StocktakeSheetCreated))]
    public async Task<IActionResult> Create(
        [FromBody] CreateStocktakeSheetRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await stocktakeService.CreateAsync(request, cancellationToken)
        );
}
