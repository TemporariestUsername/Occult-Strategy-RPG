using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>What happened when a ritual was performed.</summary>
public sealed class RitualResult
{
    public bool Performed { get; init; }
    public string? Reason { get; init; }
    public CheckResult? Check { get; init; }
    public CheckOutcome? Branch { get; init; }
    public Outcome? Outcome { get; init; }
    public EffectResult? Effects { get; init; }
}

/// <summary>
/// Performs rituals — the supernatural "tech tree". A ritual must be unlocked, its
/// requirements met, and its cost (bulk resources + specific reagents) affordable. It
/// resolves at once (a deliberate, decisive act, as opposed to a timed scheme): pay the
/// cost, apply the inherent on_perform price, run the check, apply the outcome. Whether
/// rituals should instead take turns is a balance/design call left open.
/// </summary>
public static class RitualService
{
    public static bool CanPerform(GameState state, Ritual ritual, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(ritual);
        ArgumentNullException.ThrowIfNull(rng);

        return Blocker(state, ritual, rng) is null;
    }

    public static RitualResult Perform(
        GameState state, Ritual ritual, IReadOnlyDictionary<string, string> assignment, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(ritual);
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(rng);

        string? blocked = Blocker(state, ritual, rng);
        if (blocked is not null)
        {
            return new RitualResult { Performed = false, Reason = blocked };
        }

        string? assignmentError = ValidateAssignment(state, ritual.Bindings, assignment);
        if (assignmentError is not null)
        {
            return new RitualResult { Performed = false, Reason = assignmentError };
        }

        BindingContext context = BuildContext(state, ritual.Bindings, assignment);

        ResourceLedger.Pay(state, ritual.Cost);
        ConsumeReagents(state, ritual.Reagents);

        if (ritual.OnPerform is not null)
        {
            EffectEngine.Apply(state, ritual.OnPerform, rng, context);
        }

        CheckResult? check = null;
        CheckOutcome? branch = null;
        Outcome? outcome;
        if (ritual.Check is not null)
        {
            check = CheckResolver.Resolve(state, ritual.Check, context, rng);
            branch = check.Outcome;
            outcome = SelectOutcome(ritual, check.Outcome);
        }
        else
        {
            outcome = ritual.Outcome ?? ritual.OnSuccess;
        }

        EffectResult? effects = null;
        if (outcome?.Effects is not null)
        {
            effects = EffectEngine.Apply(state, outcome.Effects, rng, context);
        }

        if (!string.IsNullOrEmpty(outcome?.NextEvent))
        {
            state.Queue.Add(new QueuedEvent { EventId = outcome!.NextEvent!, Delay = outcome.NextEventDelay });
        }

        state.Normalize();
        return new RitualResult
        {
            Performed = true,
            Check = check,
            Branch = branch,
            Outcome = outcome,
            Effects = effects,
        };
    }

    /// <summary>Why a ritual cannot be performed right now, or null if it can.</summary>
    private static string? Blocker(GameState state, Ritual ritual, IRng rng)
    {
        if (!state.UnlockedRituals.Contains(ritual.Id))
        {
            return "ritual is not unlocked";
        }

        if (ritual.Requirements is not null && !ConditionEvaluator.Evaluate(state, ritual.Requirements, rng))
        {
            return "requirements not met";
        }

        if (!ResourceLedger.CanAfford(state, ritual.Cost))
        {
            return "cannot afford cost";
        }

        return HasReagents(state, ritual.Reagents) ? null : "missing reagents";
    }

    private static Outcome? SelectOutcome(Ritual r, CheckOutcome outcome) => outcome switch
    {
        CheckOutcome.CriticalSuccess => r.OnCriticalSuccess ?? r.OnSuccess,
        CheckOutcome.Success => r.OnSuccess,
        CheckOutcome.Failure => r.OnFailure,
        CheckOutcome.CriticalFailure => r.OnCriticalFailure ?? r.OnFailure,
        _ => r.OnFailure,
    };

    private static bool HasReagents(GameState s, List<ReagentCost>? reagents) =>
        reagents is null || reagents.All(r => s.ReagentItems.GetValueOrDefault(r.Id) >= Math.Max(1, r.Count));

    private static void ConsumeReagents(GameState s, List<ReagentCost>? reagents)
    {
        if (reagents is null)
        {
            return;
        }

        foreach (ReagentCost r in reagents)
        {
            int left = s.ReagentItems.GetValueOrDefault(r.Id) - Math.Max(1, r.Count);
            if (left <= 0)
            {
                s.ReagentItems.Remove(r.Id);
            }
            else
            {
                s.ReagentItems[r.Id] = left;
            }
        }
    }

    private static string? ValidateAssignment(
        GameState state, IReadOnlyDictionary<string, Selector>? bindings, IReadOnlyDictionary<string, string> assignment)
    {
        if (bindings is null)
        {
            return null;
        }

        foreach ((string scope, Selector selector) in bindings)
        {
            bool assigned = assignment.TryGetValue(scope, out string? id) && !string.IsNullOrEmpty(id);
            if (!assigned)
            {
                if (selector.Optional)
                {
                    continue;
                }

                return $"scope '{scope}' is unassigned";
            }

            Member? member = BindingResolver.PoolFor(state, selector.From).FirstOrDefault(m => m.Id == id);
            if (member is null)
            {
                return $"performer '{id}' is not available";
            }

            if (selector.Require is not null && !selector.Require.All(l => ConditionEvaluator.EvaluateCharacter(member, l)))
            {
                return $"performer '{id}' does not meet scope '{scope}'";
            }
        }

        return null;
    }

    private static BindingContext BuildContext(
        GameState state, IReadOnlyDictionary<string, Selector>? bindings, IReadOnlyDictionary<string, string> assignment)
    {
        var context = new BindingContext();
        if (bindings is null)
        {
            return context;
        }

        foreach ((string scope, Selector selector) in bindings)
        {
            Member? member = assignment.TryGetValue(scope, out string? id) && !string.IsNullOrEmpty(id)
                ? BindingResolver.PoolFor(state, selector.From).FirstOrDefault(m => m.Id == id)
                : null;
            context.Set(scope, member);
        }

        return context;
    }
}
