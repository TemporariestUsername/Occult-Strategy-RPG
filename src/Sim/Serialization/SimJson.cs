using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaleCommunion.Sim.Serialization;

/// <summary>
/// Shared System.Text.Json settings, in one place so content and saves stay
/// consistent. Content fields are snake_case in data (e.g. <c>target_scope</c>),
/// which maps to PascalCase properties via the snake-case naming policy.
/// </summary>
public static class SimJson
{
    /// <summary>Options for reading event content (snake_case fields, lenient parsing).</summary>
    public static JsonSerializerOptions Content { get; } = BuildContent();

    /// <summary>Options for reading/writing saves (snake_case, enums as strings, indented).</summary>
    public static JsonSerializerOptions Save { get; } = BuildSave();

    private static JsonSerializerOptions BuildContent()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private static JsonSerializerOptions BuildSave()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        return options;
    }
}
