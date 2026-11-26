using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces.Auth;
using Cinema.Shared.Constants;

namespace Cinema.Api.Security;

public sealed class HttpClientContextAccessor(IHttpContextAccessor httpContextAccessor)
    : IClientContextAccessor
{
    public ClientContext Current
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context is null)
                return new ClientContext(null, null);

            return new ClientContext(
                context.Connection.RemoteIpAddress?.ToString(),
                ReadDeviceId(context)
            );
        }
    }

    private static string? ReadDeviceId(HttpContext context)
    {
        var raw = context.Request.Headers[ApiHeaders.DeviceId].ToString().Trim();
        if (raw.Length is 0 or > FieldLengths.ClientDeviceId)
            return null;

        return raw.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_') ? raw : null;
    }
}
