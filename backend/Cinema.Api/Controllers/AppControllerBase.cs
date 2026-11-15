using System.Security.Claims;
using Cinema.Application.DTOs.Common;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

public abstract class AppControllerBase : ControllerBase
{
    protected int CurrentUserId => int.Parse(User.FindFirstValue(AppClaimTypes.UserId)!);

    protected ActorContext CurrentActor =>
        new(
            CurrentUserId,
            User.FindFirstValue(AppClaimTypes.Role) ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()
        );

    protected int? CurrentUserIdOrNull
    {
        get
        {
            var raw = User.FindFirstValue(AppClaimTypes.UserId);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    protected int? CurrentEmployeeIdOrNull
    {
        get
        {
            var raw = User.FindFirstValue(AppClaimTypes.EmployeeId);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    protected static int ClampLimit(int? limit) =>
        Math.Clamp(limit ?? CursorPagination.DefaultLimit, 1, CursorPagination.MaxLimit);
}
