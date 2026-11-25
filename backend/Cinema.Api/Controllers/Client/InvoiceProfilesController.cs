using Cinema.Api.Filters;
using Cinema.Application.DTOs.Invoices;
using Cinema.Application.Interfaces.Invoices;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/invoice-profiles")]
[Authorize]
public class InvoiceProfilesController(IInvoiceProfileService invoiceProfileService)
    : AppControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceProfileDto>>> GetMine(
        CancellationToken cancellationToken
    ) => Ok(await invoiceProfileService.GetMineAsync(CurrentUserId, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_InvoiceProfileCreated))]
    public async Task<ActionResult<InvoiceProfileDto>> Create(
        [FromBody] UpsertInvoiceProfileRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await invoiceProfileService.CreateAsync(CurrentUserId, request, cancellationToken)
        );

    [HttpPut("{profileId:int}")]
    [SuccessMessage(nameof(Messages.Success_InvoiceProfileUpdated))]
    public async Task<ActionResult<InvoiceProfileDto>> Update(
        int profileId,
        [FromBody] UpsertInvoiceProfileRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await invoiceProfileService.UpdateAsync(
                CurrentUserId,
                profileId,
                request,
                cancellationToken
            )
        );

    [HttpDelete("{profileId:int}")]
    [SuccessMessage(nameof(Messages.Success_InvoiceProfileDeleted))]
    public async Task<ActionResult<DeleteInvoiceProfileResponse>> Delete(
        int profileId,
        CancellationToken cancellationToken
    ) => Ok(await invoiceProfileService.DeleteAsync(CurrentUserId, profileId, cancellationToken));
}
