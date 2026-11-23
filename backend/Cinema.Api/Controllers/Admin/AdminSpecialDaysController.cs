using Cinema.Api.Filters;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/special-days")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminSpecialDaysController(ISpecialDayService specialDayService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int? year,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await specialDayService.GetAdminAsync(
                page,
                pageSize,
                new SpecialDayListFilter(search, isActive, year),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await specialDayService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_SpecialDayCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertSpecialDayRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await specialDayService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_SpecialDayUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertSpecialDayRequest request,
        CancellationToken cancellationToken
    ) => Ok(await specialDayService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_SpecialDayDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await specialDayService.DeleteAsync(id, cancellationToken));
}
