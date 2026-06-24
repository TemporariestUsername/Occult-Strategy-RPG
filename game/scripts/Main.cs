using System;
using System.Collections.Generic;
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
/// Presentation root. Renders the order's state as a dark "case file" — semantic meter
/// bars, the initiates as people, an event card, and a running chronicle of what each
/// action did — and drives the simulation through a <see cref="GameSession"/>.
/// Presentation only: every rule lives in Sim. Built in code so it is self-contained;
/// refine the visuals (and swap in art) in the Godot editor. Palette from
/// docs/art-direction.md.
/// </summary>
public partial class Main : Control
{
    private static readonly Regex TokenRegex = new(@"\{([a-z_]+)\.([a-z_]+)\}", RegexOptions.Compiled);

    // Palette (art bible). Grounds and ink.
    private static readonly Color Parchment = Color.FromHtml("E6D8B8");
    private static readonly Color Vellum = Color.FromHtml("C8B488");
    private static readonly Color Foxing = Color.FromHtml("9A7B4F");
    private static readonly Color Bistre = Color.FromHtml("2A211A");
    private static readonly Color Midnight = Color.FromHtml("15130F");
    private static readonly Color Charcoal = Color.FromHtml("211C16");
    private static readonly Color Gold = Color.FromHtml("C9A24B");
    private static readonly Color PaleGold = Color.FromHtml("E3C879");

    // Semantic colours.
    private static readonly Color VeilColor = Color.FromHtml("5B7A8C");
    private static readonly Color OccultColor = Color.FromHtml("57998C");
    private static readonly Color MundaneColor = Color.FromHtml("A8432F");
    private static readonly Color CorruptionColor = Color.FromHtml("7C4B86");
    private static readonly Color FundsColor = Color.FromHtml("6F7A45");
    private static readonly Color LoreColor = Color.FromHtml("41507A");
    private static readonly Color ReagentColor = Color.FromHtml("7A4A33");

    private GameSession _session = null!;
    private DrawnEvent? _pending;
    private ulong _seed = 1;
    private readonly List<string> _chronicle = new();
    private readonly Dictionary<string, (ProgressBar Bar, Label Value)> _meters = new();

    private Label _status = null!;
    private Label _resources = null!;
    private RichTextLabel _roster = null!;
    private VBoxContainer _schemesBox = null!;
    private PanelContainer _eventCard = null!;
    private Label _eventTitle = null!;
    private RichTextLabel _eventBody = null!;
    private VBoxContainer _choices = null!;
    private Label _quietHint = null!;
    private RichTextLabel _chronicleLabel = null!;

    public override void _Ready()
    {
        ContentCatalog content = ContentCatalog.Load(ResolveContentDir());
        _session = new GameSession(content, _seed);

        BuildUi();
        _chronicle.Add($"[color=#C9A24B]Boston, {_session.State.Year}.[/color] Five of you, and a sixth chair left empty. The work begins.");
        Render();
    }

    private void BuildUi()
    {
        var bg = new ColorRect { Color = Midnight };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 14);
        }

        AddChild(margin);

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 14);
        margin.AddChild(columns);

        columns.AddChild(BuildLeftColumn());
        columns.AddChild(BuildRightColumn());
    }

    private Control BuildLeftColumn()
    {
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(380, 0) };
        left.AddThemeConstantOverride("separation", 12);

        _status = Heading(string.Empty, 22, Gold);
        left.AddChild(_status);

        VBoxContainer meters = Panel("The Order", out _);
        AddMeter(meters, "veil", "Veil", VeilColor);
        AddMeter(meters, "devotion", "Devotion", Gold);
        AddMeter(meters, "corruption", "Corruption", CorruptionColor);
        AddMeter(meters, "mundane", "Heat (mundane)", MundaneColor);
        AddMeter(meters, "occult", "Heat (occult)", OccultColor);
        _resources = Body(string.Empty);
        meters.AddChild(_resources);
        left.AddChild((Control)meters.GetParent());

        VBoxContainer roster = Panel("Initiates", out _);
        var rosterScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 220),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _roster = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _roster.AddThemeColorOverride("default_color", Parchment);
        rosterScroll.AddChild(_roster);
        roster.AddChild(rosterScroll);
        var rosterPanel = (Control)roster.GetParent();
        rosterPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        left.AddChild(rosterPanel);

        return left;
    }

    private Control BuildRightColumn()
    {
        var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 12);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        actions.AddChild(MakeButton("Advance Turn", OnAdvanceTurn));
        actions.AddChild(MakeButton("Draw Event", OnDrawEvent));
        actions.AddChild(MakeButton("New Campaign", OnNewCampaign));
        right.AddChild(actions);

        _eventCard = new PanelContainer();
        _eventCard.AddThemeStyleboxOverride("panel", Flat(Charcoal, Gold, 2, 5));
        var cardBox = new VBoxContainer();
        cardBox.AddThemeConstantOverride("separation", 8);
        _eventCard.AddChild(cardBox);
        _eventTitle = Heading(string.Empty, 20, PaleGold);
        cardBox.AddChild(_eventTitle);
        _eventBody = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            CustomMinimumSize = new Vector2(0, 80),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _eventBody.AddThemeColorOverride("default_color", Parchment);
        cardBox.AddChild(_eventBody);
        _choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _choices.AddThemeConstantOverride("separation", 6);
        cardBox.AddChild(_choices);
        right.AddChild(_eventCard);

        _quietHint = Body("The city is quiet. Advance the turn, run a scheme, or draw what the night brings.");
        _quietHint.AddThemeColorOverride("font_color", Foxing);
        right.AddChild(_quietHint);

        VBoxContainer schemes = Panel("Schemes", out _);
        _schemesBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _schemesBox.AddThemeConstantOverride("separation", 4);
        schemes.AddChild(_schemesBox);
        right.AddChild((Control)schemes.GetParent());

        VBoxContainer chronicle = Panel("Chronicle", out _);
        var chronicleScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 160),
        };
        _chronicleLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _chronicleLabel.AddThemeColorOverride("default_color", Vellum);
        chronicleScroll.AddChild(_chronicleLabel);
        chronicle.AddChild(chronicleScroll);
        var chroniclePanel = (Control)chronicle.GetParent();
        chroniclePanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        right.AddChild(chroniclePanel);

        return right;
    }

    private void OnAdvanceTurn()
    {
        Snap before = Snapshot();
        TurnReport report = _session.AdvanceTurn();
        Snap after = Snapshot();

        var sb = new StringBuilder();
        sb.Append($"[color=#C9A24B]Turn {_session.State.Turn} — {_session.State.Year}.[/color]");
        foreach (SchemeResolution res in report.ResolvedSchemes)
        {
            sb.Append('\n').Append(DescribeResolution(res));
        }

        AppendDeltas(sb, before, after);
        Chronicle(sb.ToString());
        Render();
    }

    private void OnDrawEvent()
    {
        _pending = _session.DrawEvent();
        if (_pending is null)
        {
            Chronicle("[color=#9A7B4F]You listen at the door, but the night offers nothing tonight.[/color]");
        }

        Render();
    }

    private void OnChoose(Choice choice)
    {
        if (_pending is null)
        {
            return;
        }

        string title = _pending.Card.Title;
        BindingContext context = _pending.Context;

        Snap before = Snapshot();
        ChoiceResolution result = _session.Choose(choice, context);
        Snap after = Snapshot();

        if (!result.Allowed)
        {
            Chronicle($"[color=#A8432F]Blocked:[/color] {result.Reason}");
            Render();
            return;
        }

        var sb = new StringBuilder();
        sb.Append($"[b][color=#E3C879]{title}[/color][/b] — {Interpolate(choice.Label, context)}");
        if (result.Check is not null)
        {
            sb.Append($"\n[color=#9A7B4F]Check {result.Check.Outcome}: rolled {result.Check.Total} vs {result.Check.Difficulty:0}.[/color]");
        }

        string? text = result.Outcome?.ResultText;
        if (!string.IsNullOrEmpty(text))
        {
            sb.Append('\n').Append(Interpolate(text, context));
        }

        AppendDeltas(sb, before, after);
        Chronicle(sb.ToString());

        _pending = null;
        Render();
    }

    private void OnStartScheme(Scheme scheme)
    {
        Snap before = Snapshot();
        SchemeStart start = _session.StartScheme(scheme);
        Snap after = Snapshot();

        if (!start.Started)
        {
            Chronicle($"[color=#A8432F]Could not begin {scheme.Title}:[/color] {start.Reason}");
            Render();
            return;
        }

        var sb = new StringBuilder();
        sb.Append($"[color=#C8B488]Set in motion:[/color] {scheme.Title}.");
        AppendDeltas(sb, before, after);
        Chronicle(sb.ToString());
        Render();
    }

    private void OnNewCampaign()
    {
        _seed++;
        _session.NewCampaign(_seed);
        _pending = null;
        _chronicle.Clear();
        _chronicle.Add($"[color=#C9A24B]A new order, a new Boston ({_session.State.Year}).[/color] The candle is lit again.");
        Render();
    }

    private void Render()
    {
        GameState state = _session.State;

        _status.Text = state.IsGameOver
            ? $"{state.City}, {state.Year} — THE ORDER HAS ENDED"
            : $"{state.City}, {state.Year} · Turn {state.Turn}";

        SetMeter("veil", state.Veil);
        SetMeter("devotion", state.Devotion);
        SetMeter("corruption", state.OrderCorruption);
        SetMeter("mundane", state.AttentionMundane);
        SetMeter("occult", state.AttentionOccult);

        _resources.Text =
            $"Funds {state.Funds:0}    Lore {state.Lore:0}    Reagents {state.Reagents:0}";

        RenderRoster(state);
        RenderSchemes();
        RenderEvent();

        _chronicleLabel.Text = string.Join("\n\n", _chronicle);
        _chronicleLabel.ScrollToLine(Math.Max(0, _chronicleLabel.GetLineCount() - 1));
    }

    private void RenderRoster(GameState state)
    {
        var sb = new StringBuilder();
        List<Member> living = state.Members.Where(m => m.IsAlive).ToList();
        if (living.Count == 0)
        {
            sb.Append("[color=#9A7B4F]No initiates remain.[/color]");
        }

        foreach (Member m in living)
        {
            string skills = string.Join(", ", m.Skills
                .Where(kv => kv.Value > 0)
                .OrderByDescending(kv => kv.Value)
                .Take(2)
                .Select(kv => $"{kv.Key} {kv.Value}"));

            sb.Append($"[b][color=#E3C879]{m.Name}[/color][/b]");
            if (m.Status != MemberStatus.Active)
            {
                sb.Append($"  [color=#A8432F]{m.Status}[/color]");
            }

            sb.Append('\n');
            sb.Append($"[color=#C8B488]{(skills.Length > 0 ? skills : "untested")}[/color]");
            if (m.Traits.Count > 0)
            {
                sb.Append($"  ·  [color=#7C4B86]{string.Join(", ", m.Traits)}[/color]");
            }

            if (m.Corruption > 0)
            {
                sb.Append($"  ·  corruption {m.Corruption:0}");
            }

            sb.Append("\n\n");
        }

        _roster.Text = sb.ToString().TrimEnd();
    }

    private void RenderSchemes()
    {
        foreach (Node child in _schemesBox.GetChildren())
        {
            child.QueueFree();
        }

        IReadOnlyList<Scheme> startable = _session.StartableSchemes();
        foreach (Scheme scheme in startable)
        {
            Scheme captured = scheme;
            _schemesBox.AddChild(MakeButton($"▸ {scheme.Title}", () => OnStartScheme(captured)));
        }

        if (startable.Count == 0)
        {
            _schemesBox.AddChild(Body("(nothing can be set in motion right now)"));
        }

        if (_session.State.ActiveSchemes.Count > 0)
        {
            _schemesBox.AddChild(Heading("In motion", 14, Foxing));
            foreach (ActiveScheme active in _session.State.ActiveSchemes)
            {
                string title = _session.Content.Schemes.TryGetValue(active.SchemeId, out Scheme? scheme)
                    ? scheme.Title
                    : active.SchemeId;
                _schemesBox.AddChild(Body($"   {title} — {active.TurnsRemaining} turn(s) left"));
            }
        }
    }

    private void RenderEvent()
    {
        foreach (Node child in _choices.GetChildren())
        {
            child.QueueFree();
        }

        bool hasEvent = _pending is not null;
        _eventCard.Visible = hasEvent;
        _quietHint.Visible = !hasEvent;
        if (_pending is null)
        {
            return;
        }

        _eventTitle.Text = _pending.Card.Title;
        _eventBody.Text = Interpolate(_pending.Card.Body, _pending.Context);

        foreach (Choice choice in _pending.Card.Choices)
        {
            Choice captured = choice;
            bool available = _session.IsChoiceAvailable(choice, _pending.Context);
            var button = new Button
            {
                Text = Interpolate(choice.Label, _pending.Context) + ChoiceAnnotation(choice, available),
                Disabled = !available,
                Alignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            button.Pressed += () => OnChoose(captured);
            _choices.AddChild(button);
        }
    }

    private static string ChoiceAnnotation(Choice choice, bool available)
    {
        var bits = new List<string>();
        if (choice.Cost is { Count: > 0 })
        {
            bits.Add(string.Join(", ", choice.Cost.Select(c => $"{c.Amount:0} {c.Resource}")));
        }

        if (choice.Check is not null)
        {
            string by = string.Join("/", new[] { choice.Check.Attribute, choice.Check.Skill }.Where(s => !string.IsNullOrEmpty(s)));
            bits.Add($"{by} check vs {choice.Check.Difficulty:0}");
        }

        if (!available && !string.IsNullOrEmpty(choice.LockedHint))
        {
            bits.Add(choice.LockedHint!);
        }

        return bits.Count == 0 ? string.Empty : $"   ({string.Join("; ", bits)})";
    }

    private string DescribeResolution(SchemeResolution res)
    {
        string title = _session.Content.Schemes.TryGetValue(res.SchemeId, out Scheme? scheme)
            ? scheme.Title
            : res.SchemeId;

        if (res.Fizzled)
        {
            return $"[color=#A8432F]{title} collapsed[/color] — an operative was gone.";
        }

        string branch = res.Branch is CheckOutcome outcome ? $" [color=#9A7B4F]({outcome})[/color]" : string.Empty;
        string? text = res.Outcome?.ResultText;
        return $"[b]{title}[/b]{branch}: {(string.IsNullOrEmpty(text) ? "done." : text)}";
    }

    private void Chronicle(string beat)
    {
        _chronicle.Add(beat);
        if (_chronicle.Count > 40)
        {
            _chronicle.RemoveAt(0);
        }
    }

    private readonly record struct Snap(
        double Funds, double Lore, double Reagents, double Veil,
        double Devotion, double Corruption, double Mundane, double Occult, int Members);

    private Snap Snapshot()
    {
        GameState s = _session.State;
        return new Snap(s.Funds, s.Lore, s.Reagents, s.Veil, s.Devotion,
            s.OrderCorruption, s.AttentionMundane, s.AttentionOccult, s.Members.Count(m => m.IsAlive));
    }

    private static void AppendDeltas(StringBuilder sb, Snap a, Snap b)
    {
        var parts = new List<string>();
        void D(string name, double x, double y)
        {
            double d = y - x;
            if (Math.Abs(d) > 0.01)
            {
                parts.Add($"{name} {(d > 0 ? "+" : "")}{d:0}");
            }
        }

        D("Funds", a.Funds, b.Funds);
        D("Lore", a.Lore, b.Lore);
        D("Reagents", a.Reagents, b.Reagents);
        D("Veil", a.Veil, b.Veil);
        D("Devotion", a.Devotion, b.Devotion);
        D("Corruption", a.Corruption, b.Corruption);
        D("Heat-mundane", a.Mundane, b.Mundane);
        D("Heat-occult", a.Occult, b.Occult);
        if (b.Members != a.Members)
        {
            parts.Add($"{(b.Members > a.Members ? "+" : "")}{b.Members - a.Members} initiate(s)");
        }

        if (parts.Count > 0)
        {
            sb.Append($"\n[color=#9A7B4F]{string.Join("  ·  ", parts)}[/color]");
        }
    }

    // ----- small themed builders -----

    private void AddMeter(VBoxContainer parent, string key, string label, Color color)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var name = new Label { Text = label, CustomMinimumSize = new Vector2(108, 0) };
        name.AddThemeColorOverride("font_color", Vellum);
        row.AddChild(name);

        var bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            ShowPercentage = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 16),
        };
        bar.AddThemeStyleboxOverride("background", Flat(Bistre, radius: 3));
        bar.AddThemeStyleboxOverride("fill", Flat(color, radius: 3));
        row.AddChild(bar);

        var value = new Label { CustomMinimumSize = new Vector2(34, 0), HorizontalAlignment = HorizontalAlignment.Right };
        value.AddThemeColorOverride("font_color", Parchment);
        row.AddChild(value);

        parent.AddChild(row);
        _meters[key] = (bar, value);
    }

    private void SetMeter(string key, double value)
    {
        if (_meters.TryGetValue(key, out (ProgressBar Bar, Label Value) meter))
        {
            meter.Bar.Value = value;
            meter.Value.Text = $"{value:0}";
        }
    }

    /// <summary>A titled, framed panel; returns its content VBox (its parent is the PanelContainer).</summary>
    private VBoxContainer Panel(string title, out PanelContainer panel)
    {
        panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", Flat(Charcoal, Bistre, 1, 4));

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        panel.AddChild(box);
        box.AddChild(Heading(title, 15, Gold));
        return box;
    }

    private Label Heading(string text, int size, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private Label Body(string text)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeColorOverride("font_color", Parchment);
        return label;
    }

    private static Button MakeButton(string text, Action onPressed)
    {
        var button = new Button { Text = text, Alignment = HorizontalAlignment.Left };
        button.Pressed += onPressed;
        return button;
    }

    private static StyleBoxFlat Flat(Color bg, Color? border = null, int borderWidth = 0, int radius = 0)
    {
        var sb = new StyleBoxFlat { BgColor = bg };
        if (border is Color b && borderWidth > 0)
        {
            sb.BorderColor = b;
            sb.SetBorderWidthAll(borderWidth);
        }

        if (radius > 0)
        {
            sb.SetCornerRadiusAll(radius);
        }

        sb.SetContentMarginAll(8);
        return sb;
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
        string fromRes = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "content"));
        if (Directory.Exists(Path.Combine(fromRes, "events")))
        {
            return fromRes;
        }

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
