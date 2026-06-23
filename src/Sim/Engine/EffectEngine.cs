using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>Outcome of applying a batch of effects: what changed, and what isn't wired up yet.</summary>
public sealed class EffectResult
{
    public List<string> Applied { get; } = new();
    public List<string> Unhandled { get; } = new();

    public bool FullyApplied => Unhandled.Count == 0;
}

/// <summary>
/// Applies the typed effect vocabulary to a <see cref="GameState"/>. This slice
/// implements the order-level economy (resources, meters, attention, institutions,
/// flags). Character/secret/patron/ritual effects are part of the vocabulary but
/// their handlers land with those systems; here they are recorded as Unhandled
/// rather than applied, so the boundary is explicit and testable (law 5).
/// </summary>
public static class EffectEngine
{
    private static readonly HashSet<string> HandledTypeSet = new(StringComparer.Ordinal)
    {
        "resource", "veil", "devotion", "order_corruption", "attention",
        "institution_influence", "institution_disposition", "set_flag",
    };

    /// <summary>The effect types this build actually simulates.</summary>
    public static IReadOnlyCollection<string> HandledTypes => HandledTypeSet;

    public static EffectResult Apply(GameState state, IEnumerable<Effect> effects, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(rng);

        var result = new EffectResult();
        foreach (Effect effect in effects)
        {
            if (ApplyOne(state, effect))
            {
                result.Applied.Add(effect.Type);
            }
            else
            {
                result.Unhandled.Add(effect.Type);
            }
        }

        state.Normalize();
        return result;
    }

    private static bool ApplyOne(GameState s, Effect e)
    {
        switch (e.Type)
        {
            case "resource":
                AddResource(s, e.Key, e.Amount ?? 0);
                return true;
            case "veil":
                s.Veil += e.Amount ?? 0;
                return true;
            case "devotion":
                s.Devotion += e.Amount ?? 0;
                return true;
            case "order_corruption":
                s.OrderCorruption += e.Amount ?? 0;
                return true;
            case "attention":
                AddAttention(s, e.Key, e.Amount ?? 0);
                return true;
            case "institution_influence":
                InstitutionFor(s, e.Key).Influence += e.Amount ?? 0;
                return true;
            case "institution_disposition":
                InstitutionFor(s, e.Key).Disposition += e.Amount ?? 0;
                return true;
            case "set_flag":
                s.Flags[Require(e.Id, "set_flag.id")] = ConditionEvaluator.AsBool(e.Value) ?? true;
                return true;
            default:
                return false; // Valid vocabulary, handler not in this build yet.
        }
    }

    private static void AddResource(GameState s, string? key, double amount)
    {
        switch (Require(key, "resource.key"))
        {
            case ContentIds.Funds: s.Funds += amount; break;
            case ContentIds.Lore: s.Lore += amount; break;
            case ContentIds.Reagents: s.Reagents += amount; break;
            default: throw new ArgumentException($"Unknown resource '{key}'.");
        }
    }

    private static void AddAttention(GameState s, string? key, double amount)
    {
        switch (Require(key, "attention.key"))
        {
            case ContentIds.AttentionMundane: s.AttentionMundane += amount; break;
            case ContentIds.AttentionOccult: s.AttentionOccult += amount; break;
            default: throw new ArgumentException($"Unknown attention channel '{key}'.");
        }
    }

    private static InstitutionState InstitutionFor(GameState s, string? key)
    {
        string id = Require(key, "institution key");
        if (!s.Institutions.TryGetValue(id, out InstitutionState? inst))
        {
            inst = new InstitutionState();
            s.Institutions[id] = inst;
        }

        return inst;
    }

    private static string Require(string? value, string what) =>
        string.IsNullOrEmpty(value)
            ? throw new ArgumentException($"Effect is missing required '{what}'.")
            : value;
}
