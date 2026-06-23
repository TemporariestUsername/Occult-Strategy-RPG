namespace PaleCommunion.Sim.Content;

/// <summary>
/// A scheme: the action economy's verb. Assign characters, let its duration elapse,
/// then it resolves via a skill check or a deterministic outcome. Mirrors
/// schemas/scheme.schema.json and reuses the shared event vocabulary
/// (Condition / Cost / Selector / Check / Outcome).
/// </summary>
public sealed class Scheme
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public string? Category { get; set; }

    public int Duration { get; set; } = 1;
    public bool Repeatable { get; set; }
    public int? CooldownTurns { get; set; }

    public Condition? Requirements { get; set; }
    public List<Cost>? Cost { get; set; }
    public Dictionary<string, Selector>? Bindings { get; set; }

    public Check? Check { get; set; }
    public Outcome? Outcome { get; set; }
    public Outcome? OnSuccess { get; set; }
    public Outcome? OnFailure { get; set; }
    public Outcome? OnCriticalSuccess { get; set; }
    public Outcome? OnCriticalFailure { get; set; }
}
