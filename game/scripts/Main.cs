using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Godot;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Game;

/// <summary>
/// Minimal presentation root. Holds the campaign state, RNG, and content, renders a
/// text summary, and drives the simulation via buttons (advance a turn, draw a pool
/// event, take a choice). Presentation only — every rule lives in Sim.
/// </summary>
public partial class Main : Control
{
    private static readonly Regex TokenRegex = new(@"\{([a-z_]+)\.([a-z_]+)\}", RegexOptions.Compiled);

    private GameState _state = null!;
    private SplitMix64Rng _rng = null!;
    private ContentCatalog _content = null!;
    private DrawnEvent? _pending;
    private string _log = string.Empty;

    private RichTextLabel _summary = null!;
    private Label _eventTitle = null!;
    private RichTextLabel _eventBody = null!;
    private VBoxContainer _choices = null!;

    public override void _Ready()
    {
        _state = GameState.NewCampaign(seed: 1);
        _rng = new SplitMix64Rng(_state.RngState);
        _content = ContentCatalog.Load(ResolveContentDir());

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

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        margin.AddChild(root);

        _summary = new RichTextLabel { FitContent = true, CustomMinimumSize = new Vector2(0, 130) };
        root.AddChild(_summary);

        var buttons = new HBoxContainer();
        root.AddChild(buttons);

        var advance = new Button { Text = "Advance Turn" };
        advance.Pressed += OnAdvanceTurn;
        buttons.AddChild(advance);

        var draw = new Button { Text = "Draw Event" };
        draw.Pressed += OnDrawEvent;
        buttons.AddChild(draw);

        _eventTitle = new Label();
        _eventTitle.AddThemeFontSizeOverride("font_size", 20);
        root.AddChild(_eventTitle);

        _eventBody = new RichTextLabel { FitContent = true, CustomMinimumSize = new Vector2(0, 90) };
        root.AddChild(_eventBody);

        _choices = new VBoxContainer();
        root.AddChild(_choices);
    }

    private void OnAdvanceTurn()
    {
        TurnReport report = TurnSystem.Advance(_state, _content.Schemes, _rng);
        _state.RngState = _rng.State;

        if (report.ResolvedSchemes.Count > 0)
        {
            _log = $"Resolved {report.ResolvedSchemes.Count} scheme(s) this turn.";
        }

        Render();
    }

    private void OnDrawEvent()
    {
        _pending = EventScheduler.Draw(_state, _content.Events, _rng);
        _state.RngState = _rng.State;

        if (_pending is null)
        {
            _log = "No event is eligible right now.";
        }
        else
        {
            EventScheduler.MarkFired(_state, _pending.Card);
            if (_pending.Card.OnAppear is not null)
            {
                EffectEngine.Apply(_state, _pending.Card.OnAppear, _rng, _pending.Context);
                _state.RngState = _rng.State;
            }

            _log = $"Drew event: {_pending.Card.Id}";
        }

        Render();
    }

    private void OnChoose(Choice choice)
    {
        if (_pending is null)
        {
            return;
        }

        ChoiceResolution result = EventResolver.Choose(_state, choice, _rng, _pending.Context);
        _state.RngState = _rng.State;
        _log = result.Allowed ? $"Chose: {choice.Id}" : $"Blocked: {result.Reason}";
        _pending = null;
        Render();
    }

    private void Render()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[b]{_state.City}[/b] — {_state.Year} (turn {_state.Turn})");
        sb.AppendLine($"Funds {_state.Funds:0}   Lore {_state.Lore:0}   Reagents {_state.Reagents:0}");
        sb.AppendLine($"Veil {_state.Veil:0}   Devotion {_state.Devotion:0}   Order corruption {_state.OrderCorruption:0}");
        sb.AppendLine($"Attention — mundane {_state.AttentionMundane:0}, occult {_state.AttentionOccult:0}");
        sb.AppendLine($"Initiates {_state.Members.Count(m => m.IsAlive)}   Active schemes {_state.ActiveSchemes.Count}");
        if (_state.IsGameOver)
        {
            sb.AppendLine($"[color=crimson]— THE ORDER HAS ENDED: {_state.Ending} —[/color]");
        }

        if (!string.IsNullOrEmpty(_log))
        {
            sb.AppendLine($"[i]{_log}[/i]");
        }

        _summary.Text = sb.ToString();

        RenderEvent();
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

        // A scratch RNG so availability checks never disturb the campaign stream.
        var scratch = new SplitMix64Rng(_state.RngState);
        foreach (Choice choice in _pending.Card.Choices)
        {
            bool available = EventResolver.IsAvailable(_state, choice, scratch, _pending.Context);
            var button = new Button
            {
                Text = Interpolate(choice.Label, _pending.Context),
                Disabled = !available,
            };
            Choice captured = choice;
            button.Pressed += () => OnChoose(captured);
            _choices.AddChild(button);
        }
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
