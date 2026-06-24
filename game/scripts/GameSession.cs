using System.Collections.Generic;
using System.Linq;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Game;

/// <summary>
/// Session orchestrator for the presentation layer: owns the live <see cref="GameState"/>,
/// the seeded RNG, and the loaded content, and sequences calls into the Sim engine. It
/// holds the RNG write-back discipline in one place. No game rules live here — every
/// rule is a Sim call; this is plain host code with no Godot dependency.
/// </summary>
public sealed class GameSession
{
    private SplitMix64Rng _rng;

    public GameSession(ContentCatalog content, ulong seed = 1)
    {
        Content = content;
        State = GameState.NewCampaign(seed);
        _rng = new SplitMix64Rng(State.RngState);
    }

    public GameState State { get; private set; }

    public ContentCatalog Content { get; }

    public void NewCampaign(ulong seed)
    {
        State = GameState.NewCampaign(seed);
        _rng = new SplitMix64Rng(State.RngState);
    }

    public TurnReport AdvanceTurn()
    {
        TurnReport report = TurnSystem.Advance(State, Content.Schemes, _rng);
        Commit();
        return report;
    }

    public DrawnEvent? DrawEvent()
    {
        DrawnEvent? drawn = EventScheduler.Draw(State, Content.Events, _rng);
        if (drawn is not null)
        {
            EventScheduler.MarkFired(State, drawn.Card);
            if (drawn.Card.OnAppear is not null)
            {
                EffectEngine.Apply(State, drawn.Card.OnAppear, _rng, drawn.Context);
            }
        }

        Commit();
        return drawn;
    }

    public ChoiceResolution Choose(Choice choice, BindingContext context)
    {
        ChoiceResolution result = EventResolver.Choose(State, choice, _rng, context);
        Commit();
        return result;
    }

    public bool IsChoiceAvailable(Choice choice, BindingContext context) =>
        EventResolver.IsAvailable(State, choice, Peek(), context);

    /// <summary>Schemes that can be started now (available and every required scope has a candidate).</summary>
    public IReadOnlyList<Scheme> StartableSchemes() =>
        Content.Schemes.Values.Where(CanStart).OrderBy(s => s.Id, System.StringComparer.Ordinal).ToList();

    public bool CanStart(Scheme scheme)
    {
        if (!SchemeService.IsAvailable(State, scheme, Peek()))
        {
            return false;
        }

        if (scheme.Bindings is null)
        {
            return true;
        }

        foreach ((string scope, Selector selector) in scheme.Bindings)
        {
            if (!selector.Optional && SchemeService.Candidates(State, scheme, scope).Count == 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Start a scheme, auto-assigning the first eligible candidate to each scope (a scaffold simplification).</summary>
    public SchemeStart StartScheme(Scheme scheme)
    {
        var assignment = new Dictionary<string, string>();
        if (scheme.Bindings is not null)
        {
            foreach (string scope in scheme.Bindings.Keys)
            {
                Member? candidate = SchemeService.Candidates(State, scheme, scope).FirstOrDefault();
                if (candidate is not null)
                {
                    assignment[scope] = candidate.Id;
                }
            }
        }

        SchemeStart result = SchemeService.Start(State, scheme, assignment, _rng);
        Commit();
        return result;
    }

    // A throwaway RNG for read-only queries, so they never consume the campaign stream.
    private SplitMix64Rng Peek() => new(State.RngState);

    private void Commit() => State.RngState = _rng.State;
}
