using PaleCommunion.Sim.Model;

namespace PaleCommunion.Sim.State;

/// <summary>
/// The entire serializable state of a campaign. Pure data plus invariant helpers;
/// no engine references. Serialized verbatim into versioned saves (law 4).
/// </summary>
public sealed class GameState
{
    /// <summary>The city that is the whole board for v1. The campaign begins in Boston.</summary>
    public string City { get; set; } = DefaultCity;

    public int Turn { get; set; }
    public int Year { get; set; } = StartYear;
    public int Tier { get; set; }

    /// <summary>Persisted RNG stream position, so determinism survives save/load.</summary>
    public ulong RngState { get; set; }

    // Bulk resources (clamped to >= 0).
    public double Funds { get; set; }
    public double Lore { get; set; }
    public double Reagents { get; set; }

    // Order meters (clamped 0..100).
    public double Veil { get; set; }
    public double Devotion { get; set; }
    public double OrderCorruption { get; set; }

    // Attention / heat (clamped 0..100).
    public double AttentionMundane { get; set; }
    public double AttentionOccult { get; set; }

    public Dictionary<string, InstitutionState> Institutions { get; set; } = new();
    public Dictionary<string, bool> Flags { get; set; } = new();
    public List<Member> Members { get; set; } = new();

    // Forward-looking collections — a stable save shape for systems still to come.
    public HashSet<string> UnlockedRituals { get; set; } = new();
    public HashSet<string> UnlockedSchemes { get; set; } = new();
    public HashSet<string> KnownSecrets { get; set; } = new();
    public HashSet<string> Relics { get; set; } = new();
    public Dictionary<string, int> ReagentItems { get; set; } = new();
    public Dictionary<string, double> PatronRelationships { get; set; } = new();
    public List<Relationship> Relationships { get; set; } = new();

    // Event flow + endgame.
    public List<QueuedEvent> Queue { get; set; } = new();
    public int RecruitCounter { get; set; }
    public string? Ending { get; set; }
    public bool IsGameOver { get; set; }

    // The Great Work (endgame). Mutually exclusive: at most one chosen per campaign.
    public string? ChosenGreatWork { get; set; }
    public int GreatWorkStep { get; set; }

    public const string DefaultCity = "Boston";
    public const int StartYear = 1905;
    public const double MeterMin = 0;
    public const double MeterMax = 100;

    /// <summary>
    /// Build a fresh campaign. Starting values here are deliberately neutral
    /// placeholders — balance/tuning is a design decision (CLAUDE.md division of
    /// labor), not something the scaffold should bake in. The starting roster is
    /// left to onboarding content.
    /// </summary>
    public static GameState NewCampaign(ulong seed, string city = DefaultCity)
    {
        var state = new GameState
        {
            City = city,
            Turn = 0,
            Year = StartYear,
            Tier = 0,
            RngState = seed,
            Funds = 100,
            Lore = 0,
            Reagents = 0,
            Veil = 80,
            Devotion = 60,
            OrderCorruption = 0,
            AttentionMundane = 0,
            AttentionOccult = 0,
        };

        foreach (string id in ContentIds.Institutions)
        {
            state.Institutions[id] = new InstitutionState { Influence = 0, Disposition = 0 };
        }

        state.Normalize();
        return state;
    }

    /// <summary>Clamp every tracked value back into its legal range. Idempotent.</summary>
    public void Normalize()
    {
        Funds = ClampMin(Funds);
        Lore = ClampMin(Lore);
        Reagents = ClampMin(Reagents);

        Veil = ClampMeter(Veil);
        Devotion = ClampMeter(Devotion);
        OrderCorruption = ClampMeter(OrderCorruption);
        AttentionMundane = ClampMeter(AttentionMundane);
        AttentionOccult = ClampMeter(AttentionOccult);

        foreach (InstitutionState inst in Institutions.Values)
        {
            inst.Influence = ClampMeter(inst.Influence);
            inst.Disposition = Math.Clamp(inst.Disposition, -100.0, 100.0);
        }

        foreach (Member m in Members)
        {
            m.Corruption = ClampMeter(m.Corruption);
        }

        foreach (Relationship r in Relationships)
        {
            r.Value = Math.Clamp(r.Value, -100.0, 100.0);
        }

        foreach (string id in PatronRelationships.Keys.ToList())
        {
            PatronRelationships[id] = Math.Clamp(PatronRelationships[id], -100.0, 100.0);
        }

        foreach (string id in ReagentItems.Keys.ToList())
        {
            if (ReagentItems[id] < 0)
            {
                ReagentItems[id] = 0;
            }
        }

        GreatWorkStep = Math.Max(0, GreatWorkStep);
    }

    public static double ClampMeter(double value) => Math.Clamp(value, MeterMin, MeterMax);

    public static double ClampMin(double value) => value < 0 ? 0 : value;
}
