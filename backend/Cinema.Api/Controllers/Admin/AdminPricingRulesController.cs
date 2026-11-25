using Cinema.Api.Filters;
using Cinema.Application.DTOs.Pricing;
using Cinema.Application.Interfaces.Pricing;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/pricing-rules")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminPricingRulesController(IPricingRuleService pricingRuleService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? ruleType,
        [FromQuery] bool? isActive,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await pricingRuleService.GetAdminAsync(
                page,
                pageSize,
                new PricingRuleListFilter(search, ruleType, isActive),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await pricingRuleService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_PricingRuleCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertPricingRuleRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await pricingRuleService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_PricingRuleUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertPricingRuleRequest request,
        CancellationToken cancellationToken
    ) => Ok(await pricingRuleService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_PricingRuleDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await pricingRuleService.DeleteAsync(id, cancellationToken));
}
