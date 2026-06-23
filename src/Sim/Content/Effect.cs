using System.Text.Json;

namespace PaleCommunion.Sim.Content;

/// <summary>
/// A single typed effect, deserialized straight from event content. The shape
/// mirrors schemas/event.schema.json. Which <see cref="Type"/> values the engine
/// actually applies is governed by <see cref="Engine.EffectEngine"/> (law 5: a
/// type exists in the schema AND gets a handler in the sim, in the same change).
/// </summary>
public sealed class Effect
{
    public string Type { get; set; } = string.Empty;
    public string? Key { get; set; }
    public string? Id { get; set; }
    public string? Scope { get; set; }
    public string? TargetScope { get; set; }
    public double? Amount { get; set; }
    public JsonElement? Value { get; set; }
    public int? Count { get; set; }
    public string? Template { get; set; }
    public string? Status { get; set; }
    public string? Kind { get; set; }
    public string? Cause { get; set; }
    public int? Steps { get; set; }
    public int? Delay { get; set; }
    public string? AboutScope { get; set; }
    public string? Ending { get; set; }
}
