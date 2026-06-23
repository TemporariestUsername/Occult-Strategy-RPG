using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class EventSchedulerTests
{
    private static EventCard Card(string json) => JsonSerializer.Deserialize<EventCard>(json, SimJson.Content)!;

    private static List<EventCard> Examples() =>
        JsonSerializer.Deserialize<List<EventCard>>(
            File.ReadAllText(TestPaths.Content("events/example_events.json")), SimJson.Content)!;

    private static EventCard Pool(string id, int priority = 0, double weight = 1) =>
        Card($"{{\"id\":\"{id}\",\"title\":\"t\",\"body\":\"b\",\"trigger\":{{\"type\":\"pool\"}}," +
             $"\"priority\":{priority},\"weight\":{weight.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
             "\"choices\":[{\"id\":\"ok\",\"label\":\"ok\",\"outcome\":{}}]}");

    private static Member Infiltrator(string id)
    {
        var m = new Member { Id = id, Name = id };
        m.Attributes["Guile"] = 3;
        m.Skills["Infiltration"] = 2;
        return m;
    }

    [Fact]
    public void Draw_PicksThePoolEvent_AndSkipsScripted()
    {
        var s = GameState.NewCampaign(1);
        s.Members.Add(Infiltrator("a"));
        s.Members.Add(Infiltrator("b")); // member_count >= 2; veil 80; state influence 0

        DrawnEvent? drawn = EventScheduler.Draw(s, Examples(), new SplitMix64Rng(3));

        Assert.NotNull(drawn);
        Assert.Equal("intrigue.magistrate_weakness", drawn!.Card.Id); // the only poolable example card
        Assert.True(drawn.Context.Has("actor"));
    }

    [Fact]
    public void ScriptedEvents_AreNeverEligible()
    {
        var s = GameState.NewCampaign(1);
        IReadOnlyList<DrawnEvent> eligible = EventScheduler.Eligible(s, Examples(), new SplitMix64Rng(1));
        Assert.DoesNotContain(eligible, e => e.Card.Id == "onboarding.empty_chair");
        Assert.DoesNotContain(eligible, e => e.Card.Id == "patron.reading_room_voice");
    }

    [Fact]
    public void MarkFired_AppliesCooldown_ForRepeatable()
    {
        var s = GameState.NewCampaign(1);
        s.Members.Add(Infiltrator("a"));
        s.Members.Add(Infiltrator("b"));
        List<EventCard> catalog = Examples();
        var rng = new SplitMix64Rng(5);

        DrawnEvent drawn = EventScheduler.Draw(s, catalog, rng)!;
        EventScheduler.MarkFired(s, drawn.Card); // magistrate is repeatable, cooldown 12

        Assert.Empty(EventScheduler.Eligible(s, catalog, rng)); // on cooldown
        s.Turn = 12;
        Assert.NotEmpty(EventScheduler.Eligible(s, catalog, rng)); // available again
    }

    [Fact]
    public void NonRepeatable_FiresOnlyOnce()
    {
        var s = GameState.NewCampaign(1);
        EventCard once = Pool("x.one");
        var catalog = new List<EventCard> { once };

        Assert.NotNull(EventScheduler.Draw(s, catalog, new SplitMix64Rng(1)));
        EventScheduler.MarkFired(s, once);
        s.Turn = 9999;
        Assert.Null(EventScheduler.Draw(s, catalog, new SplitMix64Rng(1))); // retired
    }

    [Fact]
    public void HighestPriority_Preempts()
    {
        var s = GameState.NewCampaign(1);
        var catalog = new List<EventCard> { Pool("a.low", priority: 0), Pool("b.high", priority: 10) };

        for (ulong seed = 0; seed < 20; seed++)
        {
            Assert.Equal("b.high", EventScheduler.Draw(s, catalog, new SplitMix64Rng(seed))!.Card.Id);
        }
    }

    [Fact]
    public void ZeroWeight_IsNotChosen_AgainstPositiveWeight()
    {
        var s = GameState.NewCampaign(1);
        var catalog = new List<EventCard> { Pool("a.zero", weight: 0), Pool("b.one", weight: 1) };

        for (ulong seed = 0; seed < 50; seed++)
        {
            Assert.Equal("b.one", EventScheduler.Draw(s, catalog, new SplitMix64Rng(seed))!.Card.Id);
        }
    }

    [Fact]
    public void Threat_IsEligibleOnlyUnderPressure_AndCanEndTheRun()
    {
        var s = GameState.NewCampaign(1);
        EventCard threat = Card(
            "{\"id\":\"threat.bureau_raid\",\"title\":\"The Bureau Raids the Sanctum\",\"body\":\"b\",\"category\":\"threat\"," +
            "\"trigger\":{\"type\":\"pool\",\"conditions\":{\"type\":\"attention\",\"key\":\"mundane\",\"op\":\"gte\",\"value\":50}}," +
            "\"choices\":[{\"id\":\"fall\",\"label\":\"The doors give way.\",\"outcome\":{\"effects\":[{\"type\":\"end_game\",\"ending\":\"destroyed_by_the_bureau\"}]}}]}");
        var catalog = new List<EventCard> { threat };
        var rng = new SplitMix64Rng(1);

        Assert.Null(EventScheduler.Draw(s, catalog, rng)); // attention low -> not eligible

        s.AttentionMundane = 60;
        DrawnEvent? drawn = EventScheduler.Draw(s, catalog, rng);
        Assert.NotNull(drawn);
        Assert.Equal("threat.bureau_raid", drawn!.Card.Id);

        EventResolver.Choose(s, drawn.Card.Choices[0], rng, drawn.Context);
        Assert.True(s.IsGameOver);
        Assert.Equal("destroyed_by_the_bureau", s.Ending);
    }
}
