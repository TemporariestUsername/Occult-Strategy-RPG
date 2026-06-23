namespace PaleCommunion.Sim.Model;

/// <summary>Kinds of bond between initiates. Mirrors the schema's relationship kind vocabulary.</summary>
public enum RelationshipKind
{
    Loyalty,
    Rivalry,
    Romance,
    Mentorship,
}

/// <summary>A directed bond from one member to another, intensity in -100..100.</summary>
public sealed class Relationship
{
    public string FromId { get; set; } = string.Empty;
    public string ToId { get; set; } = string.Empty;
    public RelationshipKind Kind { get; set; }
    public double Value { get; set; }
}
