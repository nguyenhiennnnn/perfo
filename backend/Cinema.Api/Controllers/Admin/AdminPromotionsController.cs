using Cinema.Api.Filters;
using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/promotions")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminPromotionsController(IPromotionService promotionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] bool? isActive,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await promotionService.GetAdminAsync(
                page,
                pageSize,
                new PromotionListFilter(search, type, isActive),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await promotionService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_PromotionCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertPromotionRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await promotionService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_PromotionUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertPromotionRequest request,
        CancellationToken cancellationToken
    ) => Ok(await promotionService.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/pause")]
    [SuccessMessage(nameof(Messages.Success_PromotionPaused))]
    public async Task<IActionResult> Pause(int id, CancellationToken cancellationToken) =>
        Ok(await promotionService.PauseAsync(id, cancellationToken));

    [HttpPost("{id:int}/resume")]
    [SuccessMessage(nameof(Messages.Success_PromotionResumed))]
    public async Task<IActionResult> Resume(int id, CancellationToken cancellationToken) =>
        Ok(await promotionService.ResumeAsync(id, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_PromotionDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await promotionService.DeleteAsync(id, cancellationToken));
}
