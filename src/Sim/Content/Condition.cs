using System.Text.Json;

namespace PaleCommunion.Sim.Content;

/// <summary>
/// A condition node: either a boolean group (all_of/any_of/none_of with child
/// <see cref="Conditions"/>) or a typed leaf. Mirrors schemas/event.schema.json.
/// </summary>
public sealed class Condition
{
    public string Type { get; set; } = string.Empty;

    // Group nodes.
    public List<Condition>? Conditions { get; set; }

    // Leaf nodes.
    public string? Key { get; set; }
    public string? Op { get; set; }
    public JsonElement? Value { get; set; }
    public string? Scope { get; set; }
    public string? Id { get; set; }

    public bool IsGroup => Type is "all_of" or "any_of" or "none_of";
}
