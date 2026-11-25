using Cinema.Api.Filters;
using Cinema.Application.DTOs.Invoices;
using Cinema.Application.Interfaces.Invoices;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/invoice-requests")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminInvoiceRequestsController(IInvoiceAdminService invoiceAdminService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] InvoiceRequestStatus? status,
        [FromQuery] string? search,
        [FromQuery] bool? needsAdjustment,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await invoiceAdminService.GetAsync(
                page,
                pageSize,
                new InvoiceRequestListFilter(status, search, needsAdjustment),
                cancellationToken
            )
        );

    [HttpPost("{id:int}/issue")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [SuccessMessage(nameof(Messages.Success_InvoiceIssued))]
    public async Task<IActionResult> Issue(
        int id,
        [FromForm] string invoiceNumber,
        IFormFile? file = null,
        CancellationToken cancellationToken = default
    )
    {
        await using var stream = file?.OpenReadStream();
        var input =
            file is null || stream is null
                ? null
                : new InvoiceFileInput(stream, file.FileName, file.Length);
        return Ok(await invoiceAdminService.IssueAsync(id, invoiceNumber, input, cancellationToken));
    }

    [HttpPut("{id:int}/file")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [SuccessMessage(nameof(Messages.Success_InvoiceFileReplaced))]
    public async Task<IActionResult> ReplaceFile(
        int id,
        IFormFile? file,
        CancellationToken cancellationToken
    )
    {
        if (file is null)
            return BadRequest();
        await using var stream = file.OpenReadStream();
        return Ok(
            await invoiceAdminService.ReplaceFileAsync(
                id,
                new InvoiceFileInput(stream, file.FileName, file.Length),
                cancellationToken
            )
        );
    }

    [HttpPost("{id:int}/reject")]
    [SuccessMessage(nameof(Messages.Success_InvoiceRejected))]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] RejectInvoiceRequest request,
        CancellationToken cancellationToken
    ) => Ok(await invoiceAdminService.RejectAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/resolve-adjustment")]
    [SuccessMessage(nameof(Messages.Success_InvoiceAdjustmentResolved))]
    public async Task<IActionResult> ResolveAdjustment(
        int id,
        [FromBody] ResolveInvoiceAdjustmentRequest request,
        CancellationToken cancellationToken
    ) => Ok(await invoiceAdminService.ResolveAdjustmentAsync(id, request, cancellationToken));

    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var file = await invoiceAdminService.DownloadAsync(id, cancellationToken);
        return File(file.Content, "application/pdf", file.FileName);
    }
}
