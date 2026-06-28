using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>What one advanced turn produced: finished schemes and events now ready to fire.</summary>
public sealed class TurnReport
{
    public int Turn { get; init; }
    public List<SchemeResolution> ResolvedSchemes { get; } = new();
    public List<string> ReadyEventIds { get; } = new();

    /// <summary>Members whose minds gave way this turn (the order's quiet Berserk).</summary>
    public List<SanityBreak> SanityBreaks { get; } = new();
}

/// <summary>
/// Advances campaign time by one turn: ticks active schemes (resolving any that finish),
/// ages the event queue (surfacing events whose delay has elapsed), and rolls the year.
/// </summary>
public static class TurnSystem
{
    /// <summary>Turns per in-game year (turns are months). A tunable balance knob.</summary>
    public const int TurnsPerYear = 12;

    public static TurnReport Advance(GameState state, IReadOnlyDictionary<string, Scheme> catalog, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(rng);

        state.Turn++;
        if (TurnsPerYear > 0 && state.Turn % TurnsPerYear == 0)
        {
            state.Year++;
        }

        var report = new TurnReport { Turn = state.Turn };

        // Tick schemes; resolve those that finish. Iterate a copy — resolution mutates state.
        foreach (ActiveScheme active in state.ActiveSchemes.ToList())
        {
            active.TurnsRemaining--;
            if (active.TurnsRemaining > 0)
            {
                continue;
            }

            state.ActiveSchemes.Remove(active);
            if (catalog.TryGetValue(active.SchemeId, out Scheme? scheme))
            {
                report.ResolvedSchemes.Add(SchemeService.Resolve(state, scheme, active, rng));
            }
            // An unknown scheme id (content removed) simply disappears.
        }

        // Age the event queue; surface events whose delay has elapsed.
        foreach (QueuedEvent queued in state.Queue.ToList())
        {
            if (queued.Delay > 0)
            {
                queued.Delay--;
                continue;
            }

            state.Queue.Remove(queued);
            report.ReadyEventIds.Add(queued.EventId);
        }

        // The month's toll: minds at the floor break (Shadow Hearts' SP / CK3 stress).
        report.SanityBreaks.AddRange(SanitySystem.ResolveBreaks(state, rng));

        state.Normalize();
        return report;
    }
}
