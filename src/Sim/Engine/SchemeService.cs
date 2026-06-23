using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>The outcome of trying to start a scheme.</summary>
public sealed class SchemeStart
{
    public bool Started { get; init; }
    public string? Reason { get; init; }
    public ActiveScheme? Scheme { get; init; }
}

/// <summary>What happened when a scheme finished.</summary>
public sealed class SchemeResolution
{
    public string SchemeId { get; init; } = string.Empty;

    /// <summary>True if an assigned operative was gone at resolution, so the scheme failed by default.</summary>
    public bool Fizzled { get; init; }

    public CheckResult? Check { get; init; }
    public CheckOutcome? Branch { get; init; }
    public Outcome? Outcome { get; init; }
    public EffectResult? Effects { get; init; }
}

/// <summary>
/// The scheme action economy: which schemes can start, starting one (assigning
/// characters and paying its cost), and resolving a finished one through the shared
/// check / effect engine. The turn loop in <see cref="TurnSystem"/> drives resolution.
/// </summary>
public static class SchemeService
{
    public static bool IsMemberBusy(GameState state, string memberId) =>
        state.ActiveSchemes.Any(a => a.Assignment.Values.Contains(memberId));

    public static bool IsAvailable(GameState state, Scheme scheme, IRng rng)
    {
        if (scheme.Requirements is not null && !ConditionEvaluator.Evaluate(state, scheme.Requirements, rng))
        {
            return false;
        }

        if (!ResourceLedger.CanAfford(state, scheme.Cost))
        {
            return false;
        }

        if (state.Turn < state.SchemeAvailableOn.GetValueOrDefault(scheme.Id))
        {
            return false;
        }

        return scheme.Repeatable || state.ActiveSchemes.All(a => a.SchemeId != scheme.Id);
    }

    /// <summary>Members eligible to fill a scope: in the selector's pool, meeting `require`, and not busy.</summary>
    public static IReadOnlyList<Member> Candidates(GameState state, Scheme scheme, string scope)
    {
        if (scheme.Bindings is null || !scheme.Bindings.TryGetValue(scope, out Selector? selector))
        {
            return Array.Empty<Member>();
        }

        return BindingResolver.PoolFor(state, selector.From)
            .Where(m => !IsMemberBusy(state, m.Id) && Matches(m, selector.Require))
            .ToList();
    }

    public static SchemeStart Start(
        GameState state, Scheme scheme, IReadOnlyDictionary<string, string> assignment, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(rng);

        if (!IsAvailable(state, scheme, rng))
        {
            return new SchemeStart { Started = false, Reason = "scheme is not available" };
        }

        if (scheme.Bindings is not null)
        {
            foreach ((string scope, Selector selector) in scheme.Bindings)
            {
                bool assigned = assignment.TryGetValue(scope, out string? memberId) && !string.IsNullOrEmpty(memberId);
                if (!assigned)
                {
                    if (selector.Optional)
                    {
                        continue;
                    }

                    return new SchemeStart { Started = false, Reason = $"scope '{scope}' is unassigned" };
                }

                Member? member = BindingResolver.PoolFor(state, selector.From).FirstOrDefault(m => m.Id == memberId);
                if (member is null)
                {
                    return new SchemeStart { Started = false, Reason = $"assigned member '{memberId}' is not available" };
                }

                if (IsMemberBusy(state, member.Id))
                {
                    return new SchemeStart { Started = false, Reason = $"member '{memberId}' is already busy" };
                }

                if (!Matches(member, selector.Require))
                {
                    return new SchemeStart { Started = false, Reason = $"member '{memberId}' does not meet scope '{scope}'" };
                }
            }
        }

        ResourceLedger.Pay(state, scheme.Cost);

        var active = new ActiveScheme { SchemeId = scheme.Id, TurnsRemaining = Math.Max(1, scheme.Duration) };
        if (scheme.Bindings is not null)
        {
            foreach (string scope in scheme.Bindings.Keys)
            {
                if (assignment.TryGetValue(scope, out string? mid) && !string.IsNullOrEmpty(mid))
                {
                    active.Assignment[scope] = mid;
                }
            }
        }

        state.ActiveSchemes.Add(active);
        state.Normalize();
        return new SchemeStart { Started = true, Scheme = active };
    }

    /// <summary>Resolve a finished scheme: rebuild its context, run the check (or outcome), apply effects.</summary>
    public static SchemeResolution Resolve(GameState state, Scheme scheme, ActiveScheme active, IRng rng)
    {
        BindingContext context = BuildContext(state, active);

        if (scheme.CooldownTurns is int cooldown and > 0)
        {
            state.SchemeAvailableOn[scheme.Id] = state.Turn + cooldown;
        }

        // A fizzle means a required operative was gone at resolution: the scheme
        // collapses with no outcome (the cost paid at the start is sunk). We do not
        // apply on_failure, since its effects may reference the now-missing scope.
        bool fizzled = scheme.Bindings is not null
            && scheme.Bindings.Any(b => !b.Value.Optional && !context.Has(b.Key));
        if (fizzled)
        {
            state.Normalize();
            return new SchemeResolution { SchemeId = scheme.Id, Fizzled = true };
        }

        CheckResult? check = null;
        CheckOutcome? branch = null;
        Outcome? outcome;
        if (scheme.Check is not null)
        {
            check = CheckResolver.Resolve(state, scheme.Check, context, rng);
            branch = check.Outcome;
            outcome = SelectOutcome(scheme, check.Outcome);
        }
        else
        {
            outcome = scheme.Outcome ?? scheme.OnSuccess;
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
        return new SchemeResolution
        {
            SchemeId = scheme.Id,
            Fizzled = false,
            Check = check,
            Branch = branch,
            Outcome = outcome,
            Effects = effects,
        };
    }

    internal static BindingContext BuildContext(GameState state, ActiveScheme active)
    {
        var context = new BindingContext();
        foreach ((string scope, string memberId) in active.Assignment)
        {
            Member? member = state.Members.FirstOrDefault(m => m.Id == memberId && m.IsAlive)
                ?? state.RecruitPool.FirstOrDefault(m => m.Id == memberId && m.IsAlive);
            context.Set(scope, member);
        }

        return context;
    }

    private static Outcome? SelectOutcome(Scheme scheme, CheckOutcome outcome) => outcome switch
    {
        CheckOutcome.CriticalSuccess => scheme.OnCriticalSuccess ?? scheme.OnSuccess,
        CheckOutcome.Success => scheme.OnSuccess,
        CheckOutcome.Failure => scheme.OnFailure,
        CheckOutcome.CriticalFailure => scheme.OnCriticalFailure ?? scheme.OnFailure,
        _ => scheme.OnFailure,
    };

    private static bool Matches(Member member, List<Condition>? require) =>
        require is null || require.All(leaf => ConditionEvaluator.EvaluateCharacter(member, leaf));
}
