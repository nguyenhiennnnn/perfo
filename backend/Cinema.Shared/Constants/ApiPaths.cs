namespace Cinema.Shared.Constants;

public class ApiPaths
{
    public const string ApiVersionPrefix = "api/v1";
    public const string AdminRoutePrefix = ApiVersionPrefix + "/admin";
    public const string AuthPath = "/" + ApiVersionPrefix + "/auth";

    public const string HubsPrefix = "/hubs";
    public const string ShowtimeHub = HubsPrefix + "/showtimes";
    public const string HangfireDashboard = "/admin/hangfire";
}
