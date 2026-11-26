using Cinema.Api.Extensions;
using Cinema.Api.Filters;
using Cinema.Application.DTOs.Abuse;
using Cinema.Application.Interfaces.Abuse;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/verification")]
[Authorize(Policy = Policies.CustomerOnly)]
public class VerificationController(ICustomerVerificationService verificationService)
    : AppControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CustomerVerificationStatusDto>> GetStatus(
        CancellationToken cancellationToken
    ) => Ok(await verificationService.GetStatusAsync(CurrentUserId, cancellationToken));

    [HttpPost("send-code")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [SuccessMessage(nameof(Messages.Success_VerificationCodeSent))]
    public async Task<ActionResult<CustomerVerificationStatusDto>> SendCode(
        CancellationToken cancellationToken
    ) => Ok(await verificationService.SendCodeAsync(CurrentUserId, cancellationToken));

    [HttpPost("confirm")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [SuccessMessage(nameof(Messages.Success_VerificationConfirmed))]
    public async Task<ActionResult<CustomerVerificationStatusDto>> Confirm(
        [FromBody] ConfirmVerificationRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await verificationService.ConfirmAsync(
                CurrentUserId,
                request.Code,
                cancellationToken
            )
        );
}
