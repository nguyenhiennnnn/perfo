using Cinema.Application.DTOs.Audit;
using Cinema.Application.Interfaces.Audit;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/audit-logs")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminAuditLogsController(IAuditLogQueryService auditLogQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] AuditLogListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await auditLogQueryService.GetAsync(filter, page, pageSize, cancellationToken));

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken) =>
        Ok(await auditLogQueryService.GetFilterOptionsAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        Ok(await auditLogQueryService.GetDetailAsync(id, cancellationToken));
}
