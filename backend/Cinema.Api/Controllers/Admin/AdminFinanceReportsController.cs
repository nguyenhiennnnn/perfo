using Cinema.Api.Extensions;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Utils;
using Cinema.Api.Filters;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/reports/finance")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminFinanceReportsController(
    IFinanceReportService financeReportService,
    IExportJobService exportJobService
) : AppControllerBase
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

    [EnableRateLimiting(RateLimitingExtensions.ExportPolicy)]
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string? format,
        CancellationToken cancellationToken
    )
    {
        var file = await exportJobService.ExportNowAsync(
            CurrentActor,
            ImportExportModules.FinanceReport,
            new FinanceExportFilter(from, to),
            ExportFormats.Parse(format, allowPdf: true),
            cancellationToken
        );
        return File(file.Content, file.ContentType, file.FileName);
    }

    [EnableRateLimiting(RateLimitingExtensions.ExportPolicy)]
    [HttpPost("export-jobs")]
    [SuccessMessage(nameof(Messages.Success_ExportJobQueued))]
    public async Task<IActionResult> CreateExportJob(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string? format,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status202Accepted,
            await exportJobService.EnqueueAsync(
                CurrentActor,
                ImportExportModules.FinanceReport,
                new FinanceExportFilter(from, to),
                ExportFormats.Parse(format, allowPdf: true),
                cancellationToken
            )
        );
}
