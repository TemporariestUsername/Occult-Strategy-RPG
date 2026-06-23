using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Saves;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class SchemeTests
{
    private static IRng Rng() => new SplitMix64Rng(0);

    private static Scheme Parse(string json) => JsonSerializer.Deserialize<Scheme>(json, SimJson.Content)!;

    private static Dictionary<string, Scheme> Catalog(params Scheme[] schemes) =>
        schemes.ToDictionary(s => s.Id);

    private static Member Infiltrator(string id, int guile, int infiltration)
    {
        var m = new Member { Id = id, Name = id };
        m.Attributes["Guile"] = guile;
        m.Skills["Infiltration"] = infiltration;
        return m;
    }

    // Assign an infiltrator, two turns, costs funds, resolves on a check.
    private const string InfiltratePolice =
        "{\"id\":\"intrigue.infiltrate_police\",\"title\":\"Infiltrate the Police\",\"duration\":2," +
        "\"cost\":[{\"resource\":\"funds\",\"amount\":20}]," +
        "\"bindings\":{\"actor\":{\"from\":\"member\",\"require\":[{\"type\":\"scope_skill\",\"key\":\"Infiltration\"}],\"prefer\":\"highest\",\"by\":\"Guile\"}}," +
        "\"check\":{\"scope\":\"actor\",\"attribute\":\"Guile\",\"skill\":\"Infiltration\",\"difficulty\":7}," +
        "\"on_success\":{\"effects\":[{\"type\":\"institution_influence\",\"key\":\"state\",\"amount\":10}]}," +
        "\"on_failure\":{\"effects\":[{\"type\":\"attention\",\"key\":\"mundane\",\"amount\":10},{\"type\":\"member_status\",\"scope\":\"actor\",\"status\":\"injured\"}]}}";

    private const string TailSuspect =
        "{\"id\":\"intrigue.tail_suspect\",\"title\":\"Tail a Suspect\",\"duration\":1," +
        "\"bindings\":{\"actor\":{\"from\":\"member\",\"require\":[{\"type\":\"scope_skill\",\"key\":\"Infiltration\"}]}}," +
        "\"outcome\":{\"effects\":[{\"type\":\"veil\",\"amount\":1}]}}";

    private const string QuickRite =
        "{\"id\":\"sanctum.quick_rite\",\"title\":\"Quick Rite\",\"duration\":1,\"repeatable\":true,\"cooldown_turns\":3," +
        "\"outcome\":{\"effects\":[{\"type\":\"resource\",\"key\":\"lore\",\"amount\":1}]}}";

    [Fact]
    public void Start_PaysCost_MarksBusy_AndQueues()
    {
        var s = GameState.NewCampaign(1);
        s.Members.Add(Infiltrator("spy", 4, 3));
        SchemeStart start = SchemeService.Start(s, Parse(InfiltratePolice),
            new Dictionary<string, string> { ["actor"] = "spy" }, Rng());

        Assert.True(start.Started);
        Assert.Equal(80, s.Funds); // 100 - 20
        Assert.Single(s.ActiveSchemes);
        Assert.True(SchemeService.IsMemberBusy(s, "spy"));
    }

    [Fact]
    public void Start_Rejects_Unaffordable_Unassigned_AndBusy()
    {
        var s = GameState.NewCampaign(1);
        s.Members.Add(Infiltrator("spy", 4, 3));
        Scheme infiltrate = Parse(InfiltratePolice);

        s.Funds = 5; // cost is 20
        Assert.False(SchemeService.Start(s, infiltrate, new Dictionary<string, string> { ["actor"] = "spy" }, Rng()).Started);

        s.Funds = 100;
        Assert.False(SchemeService.Start(s, infiltrate, new Dictionary<string, string>(), Rng()).Started); // no actor assigned

        SchemeService.Start(s, infiltrate, new Dictionary<string, string> { ["actor"] = "spy" }, Rng()); // spy now busy
        SchemeStart busy = SchemeService.Start(s, Parse(TailSuspect), new Dictionary<string, string> { ["actor"] = "spy" }, Rng());
        Assert.False(busy.Started);
        Assert.Contains("busy", busy.Reason);
        Assert.Empty(SchemeService.Candidates(s, Parse(TailSuspect), "actor")); // busy spy excluded
    }

    [Fact]
    public void TurnSystem_ResolvesAfterDuration_AppliesOutcome_AndFreesMember()
    {
        var s = GameState.NewCampaign(1);
        s.Institutions["state"].Influence = 5;
        s.Members.Add(Infiltrator("spy", 4, 3));
        Scheme scheme = Parse(InfiltratePolice);
        Dictionary<string, Scheme> catalog = Catalog(scheme);
        var rng = new SplitMix64Rng(123);

        SchemeService.Start(s, scheme, new Dictionary<string, string> { ["actor"] = "spy" }, rng);

        TurnReport t1 = TurnSystem.Advance(s, catalog, rng);
        Assert.Empty(t1.ResolvedSchemes); // duration 2 -> not yet
        Assert.Single(s.ActiveSchemes);

        TurnReport t2 = TurnSystem.Advance(s, catalog, rng);
        Assert.Single(t2.ResolvedSchemes);
        Assert.Empty(s.ActiveSchemes);
        Assert.False(SchemeService.IsMemberBusy(s, "spy"));

        SchemeResolution res = t2.ResolvedSchemes[0];
        Assert.NotNull(res.Check);
        if (res.Check!.Success)
        {
            Assert.Equal(15, s.Institutions["state"].Influence); // 5 + 10
        }
        else
        {
            Assert.True(s.AttentionMundane > 0);
        }
    }

    [Fact]
    public void Scheme_Fizzles_WhenOperativeGone_NoOutcome()
    {
        var s = GameState.NewCampaign(1);
        var spy = Infiltrator("spy", 4, 3);
        s.Members.Add(spy);
        Scheme scheme = Parse(InfiltratePolice);
        Dictionary<string, Scheme> catalog = Catalog(scheme);
        var rng = new SplitMix64Rng(1);

        SchemeService.Start(s, scheme, new Dictionary<string, string> { ["actor"] = "spy" }, rng);
        spy.Status = MemberStatus.Dead; // operative lost mid-scheme

        TurnSystem.Advance(s, catalog, rng);
        TurnReport t = TurnSystem.Advance(s, catalog, rng);

        Assert.Single(t.ResolvedSchemes);
        Assert.True(t.ResolvedSchemes[0].Fizzled);
        Assert.Equal(0, s.AttentionMundane); // on_failure NOT applied on a fizzle
    }

    [Fact]
    public void Cooldown_BlocksRestart_UntilElapsed()
    {
        var s = GameState.NewCampaign(1);
        Scheme rite = Parse(QuickRite);
        Dictionary<string, Scheme> catalog = Catalog(rite);
        var rng = new SplitMix64Rng(2);

        SchemeService.Start(s, rite, new Dictionary<string, string>(), rng);
        TurnSystem.Advance(s, catalog, rng); // turn 1: resolves; cooldown -> available on turn 4
        Assert.Equal(1, s.Lore);
        Assert.False(SchemeService.IsAvailable(s, rite, rng));

        TurnSystem.Advance(s, catalog, rng); // turn 2
        TurnSystem.Advance(s, catalog, rng); // turn 3
        Assert.False(SchemeService.IsAvailable(s, rite, rng));
        TurnSystem.Advance(s, catalog, rng); // turn 4
        Assert.True(SchemeService.IsAvailable(s, rite, rng));
    }

    [Fact]
    public void TurnSystem_AgesQueue_AndRollsYear()
    {
        var s = GameState.NewCampaign(1);
        s.Queue.Add(new QueuedEvent { EventId = "chain.next", Delay = 1 });
        var empty = new Dictionary<string, Scheme>();
        var rng = Rng();

        TurnReport t1 = TurnSystem.Advance(s, empty, rng);
        Assert.DoesNotContain("chain.next", t1.ReadyEventIds); // delay 1 -> not yet

        TurnReport t2 = TurnSystem.Advance(s, empty, rng);
        Assert.Contains("chain.next", t2.ReadyEventIds);
        Assert.Empty(s.Queue);

        var s2 = GameState.NewCampaign(1); // year 1905
        for (int i = 0; i < TurnSystem.TurnsPerYear; i++)
        {
            TurnSystem.Advance(s2, empty, rng);
        }

        Assert.Equal(1906, s2.Year);
    }

    [Fact]
    public void ActiveScheme_SurvivesSaveLoad()
    {
        var s = GameState.NewCampaign(1);
        s.Members.Add(Infiltrator("spy", 4, 3));
        SchemeService.Start(s, Parse(InfiltratePolice), new Dictionary<string, string> { ["actor"] = "spy" }, Rng());

        GameState loaded = SaveSystem.Load(SaveSystem.Save(s));

        Assert.Single(loaded.ActiveSchemes);
        Assert.Equal("intrigue.infiltrate_police", loaded.ActiveSchemes[0].SchemeId);
        Assert.Equal("spy", loaded.ActiveSchemes[0].Assignment["actor"]);
        Assert.Equal(2, loaded.ActiveSchemes[0].TurnsRemaining);
    }
}
