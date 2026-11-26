using Cinema.Api.Filters;
using Cinema.Application.DTOs.Bundles;
using Cinema.Application.Interfaces.Bundles;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/bundles")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminBundlesController(IBundleService bundleService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await bundleService.GetAdminAsync(
                page,
                pageSize,
                new BundleListFilter(search, isActive),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await bundleService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_BundleCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertBundleRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await bundleService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_BundleUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertBundleRequest request,
        CancellationToken cancellationToken
    ) => Ok(await bundleService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_BundleDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await bundleService.DeleteAsync(id, cancellationToken));
}
