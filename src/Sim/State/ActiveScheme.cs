namespace PaleCommunion.Sim.State;

/// <summary>A scheme in progress: the scheme id, the characters assigned to its scopes, and turns left.</summary>
public sealed class ActiveScheme
{
    public string SchemeId { get; set; } = string.Empty;

    /// <summary>Scope name -> assigned member id (e.g. "actor" -> "brother_ash").</summary>
    public Dictionary<string, string> Assignment { get; set; } = new();

    public int TurnsRemaining { get; set; }
}
