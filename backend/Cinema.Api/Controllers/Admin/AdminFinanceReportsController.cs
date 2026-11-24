using Cinema.Application.Interfaces;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/reports/finance")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminFinanceReportsController(IFinanceReportService financeReportService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string[]? blocks,
        CancellationToken cancellationToken
    )
    {
        var result = await financeReportService.GetAsync(from, to, blocks, cancellationToken);
        return Ok(result);
    }

    [HttpGet("blocks")]
    public IActionResult GetBlocks() => Ok(financeReportService.GetBlocks());
}
