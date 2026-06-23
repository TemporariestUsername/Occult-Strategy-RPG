namespace PaleCommunion.Sim.Content;

/// <summary>
/// Deserialized event-card models mirroring schemas/event.schema.json. These are
/// plain data; the engine (BindingResolver, CheckResolver, ConditionEvaluator,
/// EffectEngine, EventResolver) gives them behaviour. Field names map from the
/// schema's snake_case via the snake-case naming policy in SimJson.Content.
/// </summary>
public sealed class EventCard
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Art { get; set; }
    public List<string>? Tags { get; set; }
    public string? Category { get; set; }
    public double Weight { get; set; } = 1;
    public int Priority { get; set; }
    public bool Repeatable { get; set; }
    public int? CooldownTurns { get; set; }
    public Trigger? Trigger { get; set; }
    public Dictionary<string, Selector>? Bindings { get; set; }
    public List<Effect>? OnAppear { get; set; }
    public List<Choice> Choices { get; set; } = new();
}

/// <summary>Eligibility/firing rule for a card.</summary>
public sealed class Trigger
{
    public string Type { get; set; } = "pool";
    public Condition? Conditions { get; set; }
    public string? Action { get; set; }
}

/// <summary>Resolves a character into a named scope (actor, rival, …).</summary>
public sealed class Selector
{
    public string From { get; set; } = "member";
    public List<Condition>? Require { get; set; }
    public string Prefer { get; set; } = "random";
    public string? By { get; set; }
    public bool Optional { get; set; }
}

public sealed class Choice
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Tooltip { get; set; }
    public Condition? Requirements { get; set; }
    public string RequirementMode { get; set; } = "disable";
    public string? LockedHint { get; set; }
    public List<Cost>? Cost { get; set; }
    public Check? Check { get; set; }
    public Outcome? Outcome { get; set; }
    public Outcome? OnSuccess { get; set; }
    public Outcome? OnFailure { get; set; }
    public Outcome? OnCriticalSuccess { get; set; }
    public Outcome? OnCriticalFailure { get; set; }
}

/// <summary>A resource both gated on (affordability) and deducted when a choice is taken.</summary>
public sealed class Cost
{
    public string Resource { get; set; } = string.Empty;
    public double Amount { get; set; }
}

public sealed class Check
{
    public string Scope { get; set; } = "actor";
    public string? Attribute { get; set; }
    public string? Skill { get; set; }
    public double Difficulty { get; set; }
    public List<Modifier>? Modifiers { get; set; }
}

public sealed class Modifier
{
    public double Value { get; set; }
    public Condition? When { get; set; }
    public string? Note { get; set; }
}

public sealed class Outcome
{
    public string? ResultText { get; set; }
    public List<Effect>? Effects { get; set; }
    public string? NextEvent { get; set; }
    public int NextEventDelay { get; set; }
}
