using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>A pool event chosen to fire, with the characters resolved into its scopes.</summary>
public sealed class DrawnEvent
{
    public required EventCard Card { get; init; }
    public required BindingContext Context { get; init; }
}

/// <summary>
/// Draws narrative events from the pool. An event is eligible when it is a pool event
/// (no scripted/on_action trigger), off cooldown, its bindings resolve, and its trigger
/// conditions pass. Selection takes the highest-priority tier, then weighted-random
/// within it. Threats are simply pool events gated on pressure (high attention, low veil).
/// </summary>
public static class EventScheduler
{
    /// <summary>Sentinel "available on" turn for a non-repeatable event that has fired.</summary>
    public const int NeverAgain = int.MaxValue;

    public static IReadOnlyList<DrawnEvent> Eligible(
        GameState state, IReadOnlyCollection<EventCard> catalog, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(rng);

        var eligible = new List<DrawnEvent>();
        foreach (EventCard card in catalog.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            if (!IsPoolable(card))
            {
                continue;
            }

            if (state.Turn < state.EventAvailableOn.GetValueOrDefault(card.Id))
            {
                continue;
            }

            BindingResolution binding = BindingResolver.Resolve(state, card.Bindings, rng);
            if (!binding.Success)
            {
                continue;
            }

            Condition? conditions = card.Trigger?.Conditions;
            if (conditions is not null && !ConditionEvaluator.Evaluate(state, conditions, rng, binding.Context))
            {
                continue;
            }

            eligible.Add(new DrawnEvent { Card = card, Context = binding.Context });
        }

        return eligible;
    }

    /// <summary>Pick one event to fire (highest priority, then weighted-random), or null if none.</summary>
    public static DrawnEvent? Draw(GameState state, IReadOnlyCollection<EventCard> catalog, IRng rng)
    {
        IReadOnlyList<DrawnEvent> eligible = Eligible(state, catalog, rng);
        if (eligible.Count == 0)
        {
            return null;
        }

        int topPriority = eligible.Max(e => e.Card.Priority);
        List<DrawnEvent> top = eligible.Where(e => e.Card.Priority == topPriority).ToList();
        return WeightedPick(top, rng);
    }

    /// <summary>Record that an event fired: set its cooldown, or retire it if non-repeatable.</summary>
    public static void MarkFired(GameState state, EventCard card)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(card);

        state.EventAvailableOn[card.Id] = card.Repeatable
            ? state.Turn + (card.CooldownTurns ?? 0)
            : NeverAgain;
    }

    private static bool IsPoolable(EventCard card) =>
        card.Trigger is null || card.Trigger.Type == "pool";

    private static DrawnEvent WeightedPick(List<DrawnEvent> items, IRng rng)
    {
        double total = items.Sum(i => Math.Max(0, i.Card.Weight));
        if (total <= 0)
        {
            return items[rng.NextInt(0, items.Count)];
        }

        double roll = rng.NextDouble() * total;
        double cumulative = 0;
        foreach (DrawnEvent item in items)
        {
            cumulative += Math.Max(0, item.Card.Weight);
            if (roll < cumulative)
            {
                return item;
            }
        }

        return items[^1];
    }
}
