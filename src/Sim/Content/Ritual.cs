namespace PaleCommunion.Sim.Content;

/// <summary>A specific reagent item consumed by a ritual.</summary>
public sealed class ReagentCost
{
    public string Id { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
}

/// <summary>
/// A ritual: the supernatural tech-tree node. Must be unlocked, costs Lore/reagents and
/// initiates, and resolves at once into powerful effects and dangerous side effects.
/// Mirrors schemas/ritual.schema.json and reuses the shared event vocabulary.
/// </summary>
public sealed class Ritual
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }

    public Condition? Requirements { get; set; }
    public List<Cost>? Cost { get; set; }
    public List<ReagentCost>? Reagents { get; set; }
    public Dictionary<string, Selector>? Bindings { get; set; }

    /// <summary>The inherent price applied the moment the ritual is performed, before the check.</summary>
    public List<Effect>? OnPerform { get; set; }

    public Check? Check { get; set; }
    public Outcome? Outcome { get; set; }
    public Outcome? OnSuccess { get; set; }
    public Outcome? OnFailure { get; set; }
    public Outcome? OnCriticalSuccess { get; set; }
    public Outcome? OnCriticalFailure { get; set; }
}
