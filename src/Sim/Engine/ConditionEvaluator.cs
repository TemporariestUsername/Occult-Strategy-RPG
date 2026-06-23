using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>
/// Evaluates the order-level condition vocabulary against a <see cref="GameState"/>.
/// Character-scoped leaves (scope_*, member_with_*, secret/patron/relic tests) arrive
/// with the character subsystem; until then this evaluator throws on them rather than
/// guessing, so a missing handler fails loudly instead of silently passing.
/// </summary>
public static class ConditionEvaluator
{
    public static bool Evaluate(GameState state, Condition condition, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(rng);

        switch (condition.Type)
        {
            case "all_of":
                return Children(condition).All(c => Evaluate(state, c, rng));
            case "any_of":
                return Children(condition).Any(c => Evaluate(state, c, rng));
            case "none_of":
                return !Children(condition).Any(c => Evaluate(state, c, rng));
        }

        // random_chance is the one leaf that consults the RNG.
        if (condition.Type == "random_chance")
        {
            return rng.Chance(AsDouble(condition.Value) ?? 0.0);
        }

        if (condition.Type == "flag")
        {
            bool current = state.Flags.TryGetValue(condition.Id ?? string.Empty, out bool v) && v;
            bool expected = AsBool(condition.Value) ?? true;
            string op = condition.Op ?? "eq";
            return op == "neq" ? current != expected : current == expected;
        }

        double actual = ReadScalar(state, condition);
        double target = AsDouble(condition.Value) ?? 0.0;
        return Compare(actual, target, condition.Op ?? "gte");
    }

    private static IEnumerable<Condition> Children(Condition c) =>
        c.Conditions ?? Enumerable.Empty<Condition>();

    private static double ReadScalar(GameState s, Condition c)
    {
        string key = c.Key ?? string.Empty;
        return c.Type switch
        {
            "resource" => key switch
            {
                ContentIds.Funds => s.Funds,
                ContentIds.Lore => s.Lore,
                ContentIds.Reagents => s.Reagents,
                _ => throw Unknown("resource", key),
            },
            "veil" => s.Veil,
            "devotion" => s.Devotion,
            "order_corruption" => s.OrderCorruption,
            "attention" => key switch
            {
                ContentIds.AttentionMundane => s.AttentionMundane,
                ContentIds.AttentionOccult => s.AttentionOccult,
                _ => throw Unknown("attention channel", key),
            },
            "institution_influence" => Inst(s, key).Influence,
            "institution_disposition" => Inst(s, key).Disposition,
            "turn" => s.Turn,
            "year" => s.Year,
            "tier" => s.Tier,
            "member_count" => s.Members.Count(m => m.IsAlive),
            _ => throw new NotSupportedException(
                $"Condition type '{c.Type}' is not yet handled by the simulation."),
        };
    }

    private static InstitutionState Inst(GameState s, string key) =>
        s.Institutions.TryGetValue(key, out InstitutionState? inst)
            ? inst
            : throw Unknown("institution", key);

    private static bool Compare(double actual, double target, string op) => op switch
    {
        "gte" => actual >= target,
        "lte" => actual <= target,
        "gt" => actual > target,
        "lt" => actual < target,
        "eq" => actual == target,
        "neq" => actual != target,
        _ => throw new NotSupportedException($"Unknown comparison op '{op}'."),
    };

    internal static double? AsDouble(JsonElement? value)
    {
        if (value is not JsonElement e)
        {
            return null;
        }

        return e.ValueKind switch
        {
            JsonValueKind.Number => e.GetDouble(),
            JsonValueKind.String when double.TryParse(e.GetString(), out double d) => d,
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => null,
        };
    }

    internal static bool? AsBool(JsonElement? value)
    {
        if (value is not JsonElement e)
        {
            return null;
        }

        return e.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => e.GetDouble() != 0,
            JsonValueKind.String => bool.TryParse(e.GetString(), out bool b) ? b : null,
            _ => null,
        };
    }

    private static NotSupportedException Unknown(string kind, string key) =>
        new($"Unknown {kind} id '{key}'.");
}
