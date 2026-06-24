using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Godot;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Game;

/// <summary>
/// Presentation root. Renders the order's state and drives the simulation through a
/// <see cref="GameSession"/> (advance turns, draw pool events, take choices, start
/// schemes). Presentation only — every rule lives in Sim, and the session holds the
/// state/RNG/content. The layout is built in code so it is self-contained; refine the
/// visuals in the Godot editor.
/// </summary>
public partial class Main : Control
{
    private static readonly Regex TokenRegex = new(@"\{([a-z_]+)\.([a-z_]+)\}", RegexOptions.Compiled);

    private GameSession _session = null!;
    private DrawnEvent? _pending;
    private string _log = string.Empty;
    private ulong _seed = 1;

    private RichTextLabel _summary = null!;
    private VBoxContainer _schemesBox = null!;
    private Label _eventTitle = null!;
    private RichTextLabel _eventBody = null!;
    private VBoxContainer _choices = null!;

    public override void _Ready()
    {
        ContentCatalog content = ContentCatalog.Load(ResolveContentDir());
        _session = new GameSession(content, _seed);

        BuildUi();
        Render();
    }

    private void BuildUi()
    {
        var margin = new MarginContainer { AnchorRight = 1, AnchorBottom = 1 };
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var scroll = new ScrollContainer();
        margin.AddChild(scroll);

        var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(root);

        _summary = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 150),
        };
        root.AddChild(_summary);

        var actions = new HBoxContainer();
        root.AddChild(actions);
        actions.AddChild(MakeButton("Advance Turn", OnAdvanceTurn));
        actions.AddChild(MakeButton("Draw Event", OnDrawEvent));
        actions.AddChild(MakeButton("New Campaign", OnNewCampaign));

        root.AddChild(new HSeparator());
        root.AddChild(MakeHeader("Schemes"));
        _schemesBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddChild(_schemesBox);

        root.AddChild(new HSeparator());
        _eventTitle = new Label();
        _eventTitle.AddThemeFontSizeOverride("font_size", 20);
        root.AddChild(_eventTitle);

        _eventBody = new RichTextLabel
        {
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 90),
        };
        root.AddChild(_eventBody);

        _choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddChild(_choices);
    }

    private void OnAdvanceTurn()
    {
        TurnReport report = _session.AdvanceTurn();
        _log = report.ResolvedSchemes.Count > 0
            ? $"Turn {_session.State.Turn}: resolved {report.ResolvedSchemes.Count} scheme(s)."
            : $"Advanced to turn {_session.State.Turn}.";
        Render();
    }

    private void OnDrawEvent()
    {
        _pending = _session.DrawEvent();
        _log = _pending is null ? "No event is eligible right now." : $"Drew event: {_pending.Card.Id}";
        Render();
    }

    private void OnChoose(Choice choice)
    {
        if (_pending is null)
        {
            return;
        }

        ChoiceResolution result = _session.Choose(choice, _pending.Context);
        _log = result.Allowed ? $"Chose: {choice.Id}" : $"Blocked: {result.Reason}";
        _pending = null;
        Render();
    }

    private void OnStartScheme(Scheme scheme)
    {
        SchemeStart start = _session.StartScheme(scheme);
        _log = start.Started ? $"Started scheme: {scheme.Title}" : $"Could not start {scheme.Id}: {start.Reason}";
        Render();
    }

    private void OnNewCampaign()
    {
        _seed++;
        _session.NewCampaign(_seed);
        _pending = null;
        _log = $"New campaign (seed {_seed}).";
        Render();
    }

    private void Render()
    {
        RenderSummary();
        RenderSchemes();
        RenderEvent();
    }

    private void RenderSummary()
    {
        GameState state = _session.State;
        var sb = new StringBuilder();
        sb.AppendLine($"[b]{state.City}[/b] — {state.Year} (turn {state.Turn})");
        sb.AppendLine($"Funds {state.Funds:0}   Lore {state.Lore:0}   Reagents {state.Reagents:0}");
        sb.AppendLine($"Veil {state.Veil:0}   Devotion {state.Devotion:0}   Order corruption {state.OrderCorruption:0}");
        sb.AppendLine($"Attention — mundane {state.AttentionMundane:0}, occult {state.AttentionOccult:0}");
        sb.AppendLine($"Initiates {state.Members.Count(m => m.IsAlive)}   Active schemes {state.ActiveSchemes.Count}");

        string roster = string.Join(", ", state.Members.Where(m => m.IsAlive).Select(m => m.Name));
        sb.AppendLine($"Roster: {(roster.Length > 0 ? roster : "—")}");

        if (state.IsGameOver)
        {
            sb.AppendLine($"[color=crimson]— THE ORDER HAS ENDED: {state.Ending} —[/color]");
        }

        if (!string.IsNullOrEmpty(_log))
        {
            sb.AppendLine($"[i]{_log}[/i]");
        }

        _summary.Text = sb.ToString();
    }

    private void RenderSchemes()
    {
        foreach (Node child in _schemesBox.GetChildren())
        {
            child.QueueFree();
        }

        foreach (Scheme scheme in _session.StartableSchemes())
        {
            Scheme captured = scheme;
            _schemesBox.AddChild(MakeButton($"Start: {scheme.Title}", () => OnStartScheme(captured)));
        }

        if (_session.StartableSchemes().Count == 0)
        {
            _schemesBox.AddChild(new Label { Text = "(no schemes can be started now)" });
        }

        if (_session.State.ActiveSchemes.Count > 0)
        {
            _schemesBox.AddChild(MakeHeader("Active"));
            foreach (ActiveScheme active in _session.State.ActiveSchemes)
            {
                string title = _session.Content.Schemes.TryGetValue(active.SchemeId, out Scheme? scheme)
                    ? scheme.Title
                    : active.SchemeId;
                _schemesBox.AddChild(new Label { Text = $"• {title} — {active.TurnsRemaining} turn(s) left" });
            }
        }
    }

    private void RenderEvent()
    {
        foreach (Node child in _choices.GetChildren())
        {
            child.QueueFree();
        }

        if (_pending is null)
        {
            _eventTitle.Text = string.Empty;
            _eventBody.Text = string.Empty;
            return;
        }

        _eventTitle.Text = _pending.Card.Title;
        _eventBody.Text = Interpolate(_pending.Card.Body, _pending.Context);

        foreach (Choice choice in _pending.Card.Choices)
        {
            Choice captured = choice;
            var button = new Button
            {
                Text = Interpolate(choice.Label, _pending.Context),
                Disabled = !_session.IsChoiceAvailable(choice, _pending.Context),
            };
            button.Pressed += () => OnChoose(captured);
            _choices.AddChild(button);
        }
    }

    private static Button MakeButton(string text, Action onPressed)
    {
        var button = new Button { Text = text };
        button.Pressed += onPressed;
        return button;
    }

    private static Label MakeHeader(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 16);
        return label;
    }

    private static string Interpolate(string text, BindingContext context)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return TokenRegex.Replace(text, match =>
        {
            string scope = match.Groups[1].Value;
            string field = match.Groups[2].Value;
            if (context.TryGet(scope, out Member? member) && member is not null && field == "name")
            {
                return member.Name;
            }

            return match.Value;
        });
    }

    private static string ResolveContentDir()
    {
        // In the editor, res:// is the game/ folder; content/ sits beside it at the repo root.
        string fromRes = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "content"));
        if (Directory.Exists(Path.Combine(fromRes, "events")))
        {
            return fromRes;
        }

        // Fallback: walk up from the executable location to find content/.
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "content");
            if (Directory.Exists(Path.Combine(candidate, "events")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return fromRes;
    }
}
