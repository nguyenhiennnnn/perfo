using Cinema.Api.Filters;
using Cinema.Application.DTOs.Abuse;
using Cinema.Application.Interfaces.Abuse;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/customer-restrictions")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminCustomerRestrictionsController(ICustomerRestrictionService restrictionService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] CustomerRestrictionListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await restrictionService.GetAdminAsync(filter, page, pageSize, cancellationToken));

    [HttpPost("{id:int}/lift")]
    [SuccessMessage(nameof(Messages.Success_CustomerRestrictionLifted))]
    public async Task<IActionResult> Lift(int id, CancellationToken cancellationToken) =>
        Ok(await restrictionService.LiftAsync(id, cancellationToken));
}
