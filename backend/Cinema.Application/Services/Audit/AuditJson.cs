using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cinema.Application.Services.Audit;

public static class AuditJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(object value) => JsonSerializer.Serialize(value, Options);

    public static JsonObject ToObject(object value) =>
        JsonSerializer.SerializeToNode(value, Options)!.AsObject();
}
