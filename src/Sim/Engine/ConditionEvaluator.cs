using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>
/// Evaluates the condition vocabulary against a <see cref="GameState"/> (and, where a
/// leaf names a scope, a <see cref="BindingContext"/>). Great Work leaves arrive with
/// that system and throw until then, so a missing handler fails loudly.
/// </summary>
public static class ConditionEvaluator
{
    public static bool Evaluate(GameState state, Condition condition, IRng rng, BindingContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(rng);

        return condition.Type switch
        {
            "all_of" => Children(condition).All(c => Evaluate(state, c, rng, context)),
            "any_of" => Children(condition).Any(c => Evaluate(state, c, rng, context)),
            "none_of" => !Children(condition).Any(c => Evaluate(state, c, rng, context)),
            _ => EvaluateLeaf(state, condition, rng, context),
        };
    }

    /// <summary>
    /// Evaluate a character predicate leaf directly against a candidate, with no scope
    /// lookup. Used by selector <c>require</c>, whose leaves test the candidate itself.
    /// </summary>
    public static bool EvaluateCharacter(Member member, Condition leaf)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(leaf);

        return leaf.Type switch
        {
            "scope_has_trait" => member.HasTrait(leaf.Id ?? string.Empty),
            "scope_skill" => CompareOrPresence(member.GetSkill(leaf.Key ?? string.Empty), leaf),
            "scope_attribute" => CompareOrPresence(member.GetAttribute(leaf.Key ?? string.Empty), leaf),
            _ => throw new NotSupportedException(
                $"Selector 'require' does not support condition type '{leaf.Type}'."),
        };
    }

    private static bool EvaluateLeaf(GameState state, Condition c, IRng rng, BindingContext? ctx)
    {
        switch (c.Type)
        {
            case "random_chance":
                return rng.Chance(AsDouble(c.Value) ?? 0.0);
            case "flag":
            {
                bool current = state.Flags.TryGetValue(c.Id ?? string.Empty, out bool v) && v;
                bool expected = AsBool(c.Value) ?? true;
                return (c.Op ?? "eq") == "neq" ? current != expected : current == expected;
            }
            case "scope_has_trait":
                return Bound(ctx, c.Scope).HasTrait(c.Id ?? string.Empty);
            case "scope_skill":
                return CompareOrPresence(Bound(ctx, c.Scope).GetSkill(c.Key ?? string.Empty), c);
            case "scope_attribute":
                return CompareOrPresence(Bound(ctx, c.Scope).GetAttribute(c.Key ?? string.Empty), c);
            case "member_with_trait":
                return state.Members.Any(m => m.IsAlive && m.HasTrait(c.Id ?? string.Empty));
            case "member_with_skill":
                return state.Members.Any(m => m.IsAlive && CompareOrPresence(m.GetSkill(c.Key ?? string.Empty), c));
            case "secret_known":
                return state.KnownSecrets.Contains(c.Id ?? string.Empty);
            case "has_relic":
                return state.Relics.Contains(c.Id ?? string.Empty);
            case "has_reagent":
                return CompareOrPresence(state.ReagentItems.GetValueOrDefault(c.Id ?? string.Empty), c);
            case "patron_relationship":
                return Compare(state.PatronRelationships.GetValueOrDefault(c.Id ?? string.Empty),
                    AsDouble(c.Value) ?? 0.0, c.Op ?? "gte");
            case "great_work_chosen":
                return string.IsNullOrEmpty(c.Id)
                    ? state.ChosenGreatWork is not null
                    : state.ChosenGreatWork == c.Id;
            case "great_work_step":
            {
                int step = string.IsNullOrEmpty(c.Id) || state.ChosenGreatWork == c.Id ? state.GreatWorkStep : 0;
                return Compare(step, AsDouble(c.Value) ?? 0.0, c.Op ?? "gte");
            }
            default:
                return Compare(ReadScalar(state, c), AsDouble(c.Value) ?? 0.0, c.Op ?? "gte");
        }
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

    private static Member Bound(BindingContext? ctx, string? scope) =>
        ctx is not null && scope is not null && ctx.TryGet(scope, out Member? m) && m is not null
            ? m
            : throw new InvalidOperationException(
                $"Condition references scope '{scope}' which is not bound to a member.");

    private static bool CompareOrPresence(double actual, Condition c) =>
        c.Value is null ? actual >= 1 : Compare(actual, AsDouble(c.Value) ?? 0.0, c.Op ?? "gte");

    private static InstitutionState Inst(GameState s, string key) =>
        s.Institutions.TryGetValue(key, out InstitutionState? inst) ? inst : throw Unknown("institution", key);

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
