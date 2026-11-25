using Cinema.Api.Extensions;
using Cinema.Api.Filters;
using Cinema.Application.DTOs.Import;
using Cinema.Application.DTOs.ImportExport;
using Cinema.Application.DTOs.Movies;
using Cinema.Application.Interfaces.Import;
using Cinema.Application.Interfaces.Movies;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/movies")]
[Authorize]
public class AdminMoviesController(
    IMovieService movieService,
    IImportService<MovieImportRow> movieImportService
) : AppControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<IActionResult> GetMovies(
        [FromQuery] AdminMovieListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    )
    {
        var result = await movieService.GetMoviesAdminAsync(
            filter,
            page,
            pageSize,
            cancellationToken
        );

        return Ok(result);
    }

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_MovieCreated))]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Create(
        [FromBody] UpsertMovieRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await movieService.CreateMovieAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_MovieUpdated))]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertMovieRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await movieService.UpdateMovieAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_MovieDeleted))]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await movieService.DeleteMovieAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("bulk")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> BulkDelete(
        [FromBody] BulkDeleteMoviesRequest request,
        CancellationToken cancellationToken
    ) => Ok(await movieService.BulkDeleteMoviesAsync(request, cancellationToken));

    [EnableRateLimiting(RateLimitingExtensions.ImportPolicy)]
    [HttpGet("import/template")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> DownloadImportTemplate(CancellationToken cancellationToken)
    {
        var file = await movieImportService.GetTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [EnableRateLimiting(RateLimitingExtensions.ImportPolicy)]
    [HttpPost("import/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImportLimits.MaxRequestBytes)]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> PreviewImport(
        IFormFile? file = null,
        CancellationToken cancellationToken = default
    )
    {
        await using var input = file.ToImportInput();
        var result = await movieImportService.PreviewAsync(CurrentActor, input, cancellationToken);

        return Ok(result);
    }

    [EnableRateLimiting(RateLimitingExtensions.ImportPolicy)]
    [HttpPost("import/apply")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> ApplyImport(
        [FromBody] ImportApplyRequest request,
        CancellationToken cancellationToken
    )
    {
        var outcome = await movieImportService.ApplyAsync(
            CurrentActor,
            request,
            cancellationToken
        );
        return outcome.JobId is { } jobId
            ? StatusCode(StatusCodes.Status202Accepted, new ImportExportJobAcceptedDto(jobId))
            : Ok(outcome.Result);
    }

    [EnableRateLimiting(RateLimitingExtensions.ImportPolicy)]
    [HttpGet("import/errors/{previewId}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> DownloadImportErrors(
        string previewId,
        CancellationToken cancellationToken
    )
    {
        var file = await movieImportService.GetErrorFileAsync(
            CurrentUserId,
            previewId,
            cancellationToken
        );
        return File(file.Content, file.ContentType, file.FileName);
    }
}
