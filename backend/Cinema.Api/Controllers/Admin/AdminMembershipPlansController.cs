using Cinema.Api.Filters;
using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/membership-plans")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminMembershipPlansController(IMembershipPlanService planService) : ControllerBase
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
            await planService.GetAdminAsync(
                page,
                pageSize,
                new MembershipPlanListFilter(search, isActive),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await planService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_MembershipPlanCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertMembershipPlanRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await planService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_MembershipPlanUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertMembershipPlanRequest request,
        CancellationToken cancellationToken
    ) => Ok(await planService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_MembershipPlanDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await planService.DeleteAsync(id, cancellationToken));
}
