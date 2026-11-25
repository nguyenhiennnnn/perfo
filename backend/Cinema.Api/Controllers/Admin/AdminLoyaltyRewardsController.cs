using Cinema.Api.Filters;
using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/loyalty/rewards")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminLoyaltyRewardsController(ILoyaltyRewardAdminService rewardService)
    : ControllerBase
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
            await rewardService.GetAsync(
                page,
                pageSize,
                new LoyaltyRewardListFilter(search, type, isActive),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await rewardService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_LoyaltyRewardCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertLoyaltyRewardRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await rewardService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_LoyaltyRewardUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertLoyaltyRewardRequest request,
        CancellationToken cancellationToken
    ) => Ok(await rewardService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_LoyaltyRewardDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await rewardService.DeleteAsync(id, cancellationToken));
}
