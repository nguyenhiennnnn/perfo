using Cinema.Api.Extensions;
using Cinema.Application.DTOs.Import;
using Cinema.Application.DTOs.ImportExport;
using Cinema.Application.DTOs.Reconciliation;
using Cinema.Application.Interfaces.Reconciliation;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/reconciliation")]
[Authorize]
public class AdminReconciliationController(
    IReconciliationImportServiceFactory importFactory,
    IReconciliationStripeService stripeService,
    IReconciliationAdminService adminService
) : AppControllerBase
{
    [HttpGet("sessions")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetSessions(
        [FromQuery] ReconciliationSessionListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await adminService.GetSessionsAsync(filter, page, pageSize, cancellationToken));

    [HttpGet("sessions/{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetSession(int id, CancellationToken cancellationToken) =>
        Ok(await adminService.GetSessionAsync(id, cancellationToken));

    [HttpGet("items")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> GetItems(
        [FromQuery] ReconciliationItemListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await adminService.GetItemsAsync(filter, page, pageSize, cancellationToken));

    [HttpPatch("items/{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> UpdateItem(
        int id,
        [FromBody] UpdateReconciliationItemRequest request,
        CancellationToken cancellationToken
    ) => Ok(await adminService.UpdateItemAsync(id, request, CurrentUserId, cancellationToken));

    [HttpPost("stripe")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> ReconcileStripe(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status202Accepted,
            await stripeService.EnqueueAsync(CurrentActor, from, to, cancellationToken)
        );

    [EnableRateLimiting(RateLimitingExtensions.ImportPolicy)]
    [HttpGet("import/template")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> DownloadImportTemplate(
        [FromQuery] ReconciliationGateway? gateway,
        CancellationToken cancellationToken
    )
    {
        var file = await importFactory
            .Create(RequireGateway(gateway), CurrentUserId)
            .GetTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [EnableRateLimiting(RateLimitingExtensions.ImportPolicy)]
    [HttpPost("import/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImportLimits.MaxRequestBytes)]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> PreviewImport(
        [FromQuery] ReconciliationGateway? gateway,
        IFormFile? file = null,
        CancellationToken cancellationToken = default
    )
    {
        var service = importFactory.Create(RequireGateway(gateway), CurrentUserId);
        await using var input = file.ToImportInput();
        return Ok(await service.PreviewAsync(CurrentActor, input, cancellationToken));
    }

    [EnableRateLimiting(RateLimitingExtensions.ImportPolicy)]
    [HttpPost("import/apply")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> ApplyImport(
        [FromBody] ImportApplyRequest request,
        CancellationToken cancellationToken
    )
    {
        var outcome = await importFactory
            .CreateForSession()
            .ApplyAsync(CurrentActor, request, cancellationToken);
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
        var file = await importFactory
            .CreateForSession()
            .GetErrorFileAsync(CurrentUserId, previewId, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private static ReconciliationGateway RequireGateway(ReconciliationGateway? gateway) =>
        gateway
        ?? throw new ValidationAppException(
            Messages.Invalid_Reconciliation_GatewayRequired,
            new Dictionary<string, string[]> { ["gateway"] = [Messages.Invalid_Reconciliation_GatewayRequired] }
        );
}
