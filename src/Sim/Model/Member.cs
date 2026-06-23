using System.Text.Json.Serialization;

namespace PaleCommunion.Sim.Model;

/// <summary>Lifecycle/status of an initiate. Mirrors the schema's member status vocabulary.</summary>
public enum MemberStatus
{
    Active,
    Injured,
    Maddened,
    Imprisoned,
    Missing,
    Exalted,
    Recovered,
    Dead,
}

/// <summary>
/// A named initiate. Deliberately minimal for now — the full character RPG layer
/// (attributes, skills, traits, relationships) lands with its own schema and
/// systems. Present here so order-level rules (e.g. member_count) and saves have a
/// stable shape from day one.
/// </summary>
public sealed class Member
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    /// <summary>Per-character corruption, 0..100.</summary>
    public double Corruption { get; set; }

    [JsonIgnore]
    public bool IsAlive => Status != MemberStatus.Dead;
}

/// <summary>One institution's standing toward the order.</summary>
public sealed class InstitutionState
{
    /// <summary>The order's reach into this institution, 0..100.</summary>
    public double Influence { get; set; }

    /// <summary>How the institution feels about the order, -100..100.</summary>
    public double Disposition { get; set; }
}
