using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>Result of resolving a card's bindings: the filled context and whether the card qualifies.</summary>
public sealed class BindingResolution
{
    public BindingContext Context { get; } = new();

    /// <summary>False if a non-optional scope matched no candidate — the event is disqualified.</summary>
    public bool Success { get; internal set; } = true;

    /// <summary>Non-optional scopes that could not be filled.</summary>
    public List<string> Unfilled { get; } = new();
}

/// <summary>
/// Resolves a card's <c>bindings</c> into concrete characters, deterministically via
/// the seeded RNG. A selector draws from a candidate pool, filters by its `require`
/// predicates (evaluated against each candidate), and picks by `prefer`
/// (random / highest / lowest by a named stat).
/// </summary>
public static class BindingResolver
{
    public static BindingResolution Resolve(
        GameState state, IReadOnlyDictionary<string, Selector>? bindings, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(rng);

        var result = new BindingResolution();
        if (bindings is null)
        {
            return result;
        }

        foreach ((string scope, Selector selector) in bindings)
        {
            Member? chosen = Select(state, selector, rng);
            result.Context.Set(scope, chosen);
            if (chosen is null && !selector.Optional)
            {
                result.Success = false;
                result.Unfilled.Add(scope);
            }
        }

        return result;
    }

    private static Member? Select(GameState state, Selector selector, IRng rng)
    {
        List<Member> matches = Pool(state, selector.From)
            .Where(m => Matches(m, selector.Require))
            .ToList();

        if (matches.Count == 0)
        {
            return null;
        }

        return selector.Prefer switch
        {
            "highest" => ByStat(matches, selector.By, highest: true),
            "lowest" => ByStat(matches, selector.By, highest: false),
            _ => matches[rng.NextInt(0, matches.Count)],
        };
    }

    private static IEnumerable<Member> Pool(GameState state, string from) => from switch
    {
        "member" => state.Members.Where(m => m.IsAlive),

        // recruit_pool, rival_order, npc, institution_contact are not modelled yet;
        // they resolve to an empty pool until those systems land.
        _ => Enumerable.Empty<Member>(),
    };

    private static bool Matches(Member candidate, List<Condition>? require)
    {
        if (require is null)
        {
            return true;
        }

        foreach (Condition leaf in require)
        {
            if (!ConditionEvaluator.EvaluateCharacter(candidate, leaf))
            {
                return false;
            }
        }

        return true;
    }

    private static Member ByStat(List<Member> matches, string? by, bool highest)
    {
        if (string.IsNullOrEmpty(by))
        {
            return matches[0];
        }

        // Deterministic: order by the stat, break ties by id.
        List<Member> ordered = matches
            .OrderBy(m => m.GetStat(by))
            .ThenBy(m => m.Id, StringComparer.Ordinal)
            .ToList();

        return highest ? ordered[^1] : ordered[0];
    }
}
