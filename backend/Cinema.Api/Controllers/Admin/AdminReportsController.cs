using Cinema.Api.Extensions;
using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/reports")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminReportsController(
    IReportService reportService,
    IReportExportService reportExportService,
    IExportJobService exportJobService
) : AppControllerBase
{
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] string groupBy,
        CancellationToken cancellationToken
    )
    {
        var result = await reportService.GetRevenueReportAsync(
            fromDate,
            toDate,
            groupBy,
            cancellationToken
        );
        return Ok(result);
    }

    [HttpGet("top-movies")]
    public async Task<IActionResult> GetTopMovies(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] int? limit,
        CancellationToken cancellationToken
    )
    {
        var effectiveLimit = ResolveLimit(limit);
        var result = await reportService.GetTopMoviesReportAsync(
            fromDate,
            toDate,
            effectiveLimit,
            cancellationToken
        );
        return Ok(result);
    }

    [HttpGet("employee-performance")]
    public async Task<IActionResult> GetEmployeePerformance(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken
    )
    {
        var result = await reportService.GetEmployeePerformanceReportAsync(
            fromDate,
            toDate,
            cancellationToken
        );
        return Ok(result);
    }

    [EnableRateLimiting(RateLimitingExtensions.ExportPolicy)]
    [HttpGet("revenue/export")]
    public async Task<IActionResult> ExportRevenue(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] string groupBy,
        [FromQuery] string? format,
        CancellationToken cancellationToken
    )
    {
        var exportFormat = ExportFormats.Parse(format, allowPdf: true);
        var file = await exportJobService.RunTrackedAsync(
            CurrentActor,
            ImportExportModules.RevenueReport,
            exportFormat,
            new
            {
                fromDate,
                toDate,
                groupBy,
            },
            (_, _) =>
                reportExportService.ExportRevenueAsync(
                    fromDate,
                    toDate,
                    groupBy,
                    exportFormat,
                    cancellationToken
                ),
            cancellationToken
        );
        return ToFileResult(file);
    }

    [EnableRateLimiting(RateLimitingExtensions.ExportPolicy)]
    [HttpGet("top-movies/export")]
    public async Task<IActionResult> ExportTopMovies(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] int? limit,
        [FromQuery] string? format,
        CancellationToken cancellationToken
    )
    {
        var exportFormat = ExportFormats.Parse(format, allowPdf: true);
        var resolvedLimit = ResolveLimit(limit);
        var file = await exportJobService.RunTrackedAsync(
            CurrentActor,
            ImportExportModules.TopMoviesReport,
            exportFormat,
            new
            {
                fromDate,
                toDate,
                limit = resolvedLimit,
            },
            (_, _) =>
                reportExportService.ExportTopMoviesAsync(
                    fromDate,
                    toDate,
                    resolvedLimit,
                    exportFormat,
                    cancellationToken
                ),
            cancellationToken
        );
        return ToFileResult(file);
    }

    [EnableRateLimiting(RateLimitingExtensions.ExportPolicy)]
    [HttpGet("employee-performance/export")]
    public async Task<IActionResult> ExportEmployeePerformance(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] string? format,
        CancellationToken cancellationToken
    )
    {
        var exportFormat = ExportFormats.Parse(format, allowPdf: true);
        var file = await exportJobService.RunTrackedAsync(
            CurrentActor,
            ImportExportModules.EmployeePerformanceReport,
            exportFormat,
            new { fromDate, toDate },
            (_, _) =>
                reportExportService.ExportEmployeePerformanceAsync(
                    fromDate,
                    toDate,
                    exportFormat,
                    cancellationToken
                ),
            cancellationToken
        );
        return ToFileResult(file);
    }

    private static int ResolveLimit(int? limit) =>
        Math.Clamp(limit ?? Pagination.DefaultPageSize, 1, Pagination.MaxPageSize);

    private FileStreamResult ToFileResult(ExportFileDto file) =>
        File(file.Content, file.ContentType, file.FileName);
}
