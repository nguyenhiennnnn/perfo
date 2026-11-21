using System.Text.Json;
using Cinema.Application.Utils;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.ImportExport;

public static class ImportExportJson
{
    private static readonly JsonSerializerOptions Options = Create();

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new InvalidOperationException(
            string.Format(Messages.Json_FilterDeserializeFailed, typeof(T).Name)
        );

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new DateOnlyJsonConverter());
        return options;
    }
}
