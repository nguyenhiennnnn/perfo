using Cinema.Api.Filters;
using Cinema.Application.DTOs.Loyalty;
using Cinema.Application.Interfaces.Loyalty;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/loyalty/challenges")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminLoyaltyChallengesController(ILoyaltyChallengeAdminService challengeService)
    : ControllerBase
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
            await challengeService.GetAsync(
                page,
                pageSize,
                new LoyaltyChallengeListFilter(search, isActive),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await challengeService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_LoyaltyChallengeCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertLoyaltyChallengeRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await challengeService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_LoyaltyChallengeUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertLoyaltyChallengeRequest request,
        CancellationToken cancellationToken
    ) => Ok(await challengeService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_LoyaltyChallengeDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await challengeService.DeleteAsync(id, cancellationToken));
}
