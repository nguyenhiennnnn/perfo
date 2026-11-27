using Cinema.Application.Interfaces.Reports;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/reports/retention")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminRetentionReportsController(IRetentionReportService reportService)
    : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int months,
        CancellationToken cancellationToken
    ) => Ok(await reportService.GetAsync(months, cancellationToken));
}
