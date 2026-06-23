using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>The result of taking a choice: which branch fired, what it did, and any chained event.</summary>
public sealed class ChoiceResolution
{
    public bool Allowed { get; init; }
    public string? Reason { get; init; }
    public CheckResult? Check { get; init; }
    public CheckOutcome? Branch { get; init; }
    public Outcome? Outcome { get; init; }
    public EffectResult? Effects { get; init; }
    public string? NextEvent { get; init; }
}

/// <summary>
/// Drives a single event card end-to-end: resolve its bindings and on-appear effects,
/// test eligibility and per-choice availability, then take a choice — pay its cost,
/// resolve a skill check (or its deterministic outcome), apply the resulting effects
/// through the binding context, and queue any chained event.
/// </summary>
public static class EventResolver
{
    /// <summary>Resolve bindings and run on_appear effects. Begin a card before presenting it.</summary>
    public static BindingResolution Begin(GameState state, EventCard card, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(rng);

        BindingResolution binding = BindingResolver.Resolve(state, card.Bindings, rng);
        if (binding.Success && card.OnAppear is not null)
        {
            EffectEngine.Apply(state, card.OnAppear, rng, binding.Context);
        }

        return binding;
    }

    /// <summary>Whether the card's trigger conditions currently pass.</summary>
    public static bool IsEligible(GameState state, EventCard card, IRng rng, BindingContext? context)
    {
        Condition? conditions = card.Trigger?.Conditions;
        return conditions is null || ConditionEvaluator.Evaluate(state, conditions, rng, context);
    }

    /// <summary>Whether a choice may be taken right now (requirements met and cost affordable).</summary>
    public static bool IsAvailable(GameState state, Choice choice, IRng rng, BindingContext? context)
    {
        if (choice.Requirements is not null && !ConditionEvaluator.Evaluate(state, choice.Requirements, rng, context))
        {
            return false;
        }

        return CanAfford(state, choice.Cost);
    }

    public static ChoiceResolution Choose(GameState state, Choice choice, IRng rng, BindingContext? context)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(choice);
        ArgumentNullException.ThrowIfNull(rng);

        if (choice.Requirements is not null && !ConditionEvaluator.Evaluate(state, choice.Requirements, rng, context))
        {
            return new ChoiceResolution { Allowed = false, Reason = "requirements not met" };
        }

        if (!CanAfford(state, choice.Cost))
        {
            return new ChoiceResolution { Allowed = false, Reason = "cannot afford cost" };
        }

        PayCost(state, choice.Cost);

        CheckResult? checkResult = null;
        CheckOutcome? branch = null;
        Outcome? outcome;
        if (choice.Check is not null)
        {
            checkResult = CheckResolver.Resolve(state, choice.Check, context, rng);
            branch = checkResult.Outcome;
            outcome = SelectOutcome(choice, checkResult.Outcome);
        }
        else
        {
            outcome = choice.Outcome;
        }

        EffectResult? effects = null;
        if (outcome?.Effects is not null)
        {
            effects = EffectEngine.Apply(state, outcome.Effects, rng, context);
        }

        string? next = outcome?.NextEvent;
        if (!string.IsNullOrEmpty(next))
        {
            state.Queue.Add(new QueuedEvent { EventId = next, Delay = outcome!.NextEventDelay });
        }

        state.Normalize();
        return new ChoiceResolution
        {
            Allowed = true,
            Check = checkResult,
            Branch = branch,
            Outcome = outcome,
            Effects = effects,
            NextEvent = next,
        };
    }

    private static Outcome? SelectOutcome(Choice choice, CheckOutcome outcome) => outcome switch
    {
        CheckOutcome.CriticalSuccess => choice.OnCriticalSuccess ?? choice.OnSuccess,
        CheckOutcome.Success => choice.OnSuccess,
        CheckOutcome.Failure => choice.OnFailure,
        CheckOutcome.CriticalFailure => choice.OnCriticalFailure ?? choice.OnFailure,
        _ => choice.OnFailure,
    };

    private static bool CanAfford(GameState state, List<Cost>? cost)
    {
        if (cost is null)
        {
            return true;
        }

        return cost.All(c => ResourceValue(state, c.Resource) >= c.Amount);
    }

    private static void PayCost(GameState state, List<Cost>? cost)
    {
        if (cost is null)
        {
            return;
        }

        foreach (Cost c in cost)
        {
            PayResource(state, c.Resource, c.Amount);
        }

        state.Normalize();
    }

    private static double ResourceValue(GameState s, string resource) => resource switch
    {
        ContentIds.Funds => s.Funds,
        ContentIds.Lore => s.Lore,
        ContentIds.Reagents => s.Reagents,
        _ => throw new ArgumentException($"Unknown cost resource '{resource}'."),
    };

    private static void PayResource(GameState s, string resource, double amount)
    {
        switch (resource)
        {
            case ContentIds.Funds: s.Funds -= amount; break;
            case ContentIds.Lore: s.Lore -= amount; break;
            case ContentIds.Reagents: s.Reagents -= amount; break;
            default: throw new ArgumentException($"Unknown cost resource '{resource}'.");
        }
    }
}
