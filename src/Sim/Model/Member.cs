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
/// A named initiate — the heart of the order. Carries the fixed attribute and skill
/// vocabularies (keyed by their schema names, e.g. "Guile", "Infiltration"; an absent
/// entry reads as 0), a set of trait content ids, a status, and a personal corruption
/// track.
/// </summary>
public sealed class Member
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    /// <summary>Per-character corruption, 0..100.</summary>
    public double Corruption { get; set; }

    /// <summary>
    /// Soundness of mind, 0..100 (100 = sound). Occult work, ritual, and a Pact's
    /// whispers erode it; at the floor the member breaks (see <c>SanitySystem</c>).
    /// </summary>
    public double Sanity { get; set; } = 100;

    /// <summary>
    /// Personal Law(−)/Chaos(+) leaning, −100..100 (0 = Neutral). The order's own
    /// alignment pulls against members who lean the other way (schism — a later system).
    /// </summary>
    public double Alignment { get; set; }

    /// <summary>Attributes keyed by schema name: Intellect, Will, Presence, Guile, Body.</summary>
    public Dictionary<string, int> Attributes { get; set; } = new();

    /// <summary>Skills keyed by schema name: Lore, Ritual, Infiltration, Persuasion, Violence, Medicine, Finance.</summary>
    public Dictionary<string, int> Skills { get; set; } = new();

    /// <summary>Trait content ids (e.g. "aristocrat").</summary>
    public HashSet<string> Traits { get; set; } = new();

    [JsonIgnore]
    public bool IsAlive => Status != MemberStatus.Dead;

    /// <summary>This member's pole on the Law–Chaos axis, derived from <see cref="Alignment"/>.</summary>
    [JsonIgnore]
    public AlignmentPole Leaning => AlignmentScale.Classify(Alignment);

    public int GetAttribute(string name) => Attributes.TryGetValue(name, out int v) ? v : 0;

    public int GetSkill(string name) => Skills.TryGetValue(name, out int v) ? v : 0;

    /// <summary>Attribute or skill value by name (attributes win on a name clash); 0 if neither exists.</summary>
    public int GetStat(string name) =>
        Attributes.TryGetValue(name, out int a) ? a : (Skills.TryGetValue(name, out int s) ? s : 0);

    public bool HasTrait(string id) => Traits.Contains(id);
}

/// <summary>One institution's standing toward the order.</summary>
public sealed class InstitutionState
{
    /// <summary>The order's reach into this institution, 0..100.</summary>
    public double Influence { get; set; }

    /// <summary>How the institution feels about the order, -100..100.</summary>
    public double Disposition { get; set; }
}
