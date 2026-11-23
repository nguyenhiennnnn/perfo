using Cinema.Api.Filters;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/inventory/recipes")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminComboRecipesController(IComboRecipeService comboRecipeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await comboRecipeService.GetAdminAsync(
                page,
                pageSize,
                new ComboRecipeListFilter(search),
                cancellationToken
            )
        );

    [HttpGet("{comboId:int}")]
    public async Task<IActionResult> GetByComboId(
        int comboId,
        CancellationToken cancellationToken
    ) => Ok(await comboRecipeService.GetByComboIdAsync(comboId, cancellationToken));

    [HttpPut("{comboId:int}")]
    [SuccessMessage(nameof(Messages.Success_ComboRecipeSaved))]
    public async Task<IActionResult> Upsert(
        int comboId,
        [FromBody] UpsertComboRecipeRequest request,
        CancellationToken cancellationToken
    ) => Ok(await comboRecipeService.UpsertAsync(comboId, request, cancellationToken));

    [HttpDelete("{comboId:int}")]
    [SuccessMessage(nameof(Messages.Success_ComboRecipeDeleted))]
    public async Task<IActionResult> Delete(int comboId, CancellationToken cancellationToken) =>
        Ok(await comboRecipeService.DeleteAsync(comboId, cancellationToken));
}
