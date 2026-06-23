using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class RitualTests
{
    private static IRng Rng() => new SplitMix64Rng(0);

    private static Ritual Parse(string json) => JsonSerializer.Deserialize<Ritual>(json, SimJson.Content)!;

    // Costs Lore + a reagent, assigns a ritualist, pays an inherent corruption/attention
    // price on performing, then resolves on a Will+Ritual check.
    private const string BorrowedName =
        "{\"id\":\"the_borrowed_name\",\"title\":\"The Borrowed Name\"," +
        "\"cost\":[{\"resource\":\"lore\",\"amount\":10}]," +
        "\"reagents\":[{\"id\":\"graveyard_dirt\",\"count\":1}]," +
        "\"bindings\":{\"actor\":{\"from\":\"member\",\"require\":[{\"type\":\"scope_skill\",\"key\":\"Ritual\"}],\"prefer\":\"highest\",\"by\":\"Will\"}}," +
        "\"on_perform\":[{\"type\":\"order_corruption\",\"amount\":5},{\"type\":\"attention\",\"key\":\"occult\",\"amount\":5}]," +
        "\"check\":{\"scope\":\"actor\",\"attribute\":\"Will\",\"skill\":\"Ritual\",\"difficulty\":8}," +
        "\"on_success\":{\"effects\":[{\"type\":\"resource\",\"key\":\"lore\",\"amount\":20},{\"type\":\"member_corruption\",\"scope\":\"actor\",\"amount\":10}]}," +
        "\"on_failure\":{\"effects\":[{\"type\":\"member_status\",\"scope\":\"actor\",\"status\":\"maddened\"},{\"type\":\"attention\",\"key\":\"occult\",\"amount\":15}]}}";

    private static Member Ritualist(string id, int will, int ritual)
    {
        var m = new Member { Id = id, Name = id };
        m.Attributes["Will"] = will;
        m.Skills["Ritual"] = ritual;
        return m;
    }

    private static GameState Ready()
    {
        var s = GameState.NewCampaign(1);
        s.Lore = 30;
        s.ReagentItems["graveyard_dirt"] = 2;
        s.UnlockedRituals.Add("the_borrowed_name");
        s.Members.Add(Ritualist("adept", will: 4, ritual: 4));
        return s;
    }

    private static Dictionary<string, string> Assign() => new() { ["actor"] = "adept" };

    [Fact]
    public void Perform_Blocked_WhenNotUnlocked()
    {
        var s = Ready();
        s.UnlockedRituals.Clear();
        RitualResult r = RitualService.Perform(s, Parse(BorrowedName), Assign(), Rng());

        Assert.False(r.Performed);
        Assert.Equal("ritual is not unlocked", r.Reason);
        Assert.Equal(30, s.Lore); // nothing spent
    }

    [Fact]
    public void Perform_Blocked_WhenMissingReagents()
    {
        var s = Ready();
        s.ReagentItems.Remove("graveyard_dirt");
        RitualResult r = RitualService.Perform(s, Parse(BorrowedName), Assign(), Rng());

        Assert.False(r.Performed);
        Assert.Equal("missing reagents", r.Reason);
    }

    [Fact]
    public void Perform_PaysCost_ConsumesReagent_AppliesInherentPrice_AndResolves()
    {
        var s = Ready();
        RitualResult r = RitualService.Perform(s, Parse(BorrowedName), Assign(), new SplitMix64Rng(2024));

        Assert.True(r.Performed);
        Assert.NotNull(r.Check);

        // Inherent price applied regardless of the check outcome.
        Assert.Equal(5, s.OrderCorruption);
        Assert.Equal(1, s.ReagentItems["graveyard_dirt"]); // 2 - 1 consumed
        Assert.True(s.AttentionOccult >= 5);

        Member adept = s.Members.Single(m => m.Id == "adept");
        if (r.Check!.Success)
        {
            Assert.Equal(40, s.Lore);              // 30 - 10 cost + 20 boon
            Assert.Equal(10, adept.Corruption);
        }
        else
        {
            Assert.Equal(20, s.Lore);              // 30 - 10 cost, no boon
            Assert.Equal(MemberStatus.Maddened, adept.Status);
            Assert.True(s.AttentionOccult >= 20);  // 5 inherent + 15 backlash
        }
    }

    [Fact]
    public void CanPerform_ReflectsGates()
    {
        var s = Ready();
        Ritual ritual = Parse(BorrowedName);
        Assert.True(RitualService.CanPerform(s, ritual, Rng()));

        s.Lore = 0; // cannot afford the lore cost
        Assert.False(RitualService.CanPerform(s, ritual, Rng()));
    }

    [Fact]
    public void Perform_Blocked_WhenPerformerLacksSkill()
    {
        var s = Ready();
        s.Members.Clear();
        s.Members.Add(new Member { Id = "novice", Name = "novice" }); // no Ritual skill
        RitualResult r = RitualService.Perform(s, Parse(BorrowedName), new Dictionary<string, string> { ["actor"] = "novice" }, Rng());

        Assert.False(r.Performed);
        Assert.Contains("does not meet", r.Reason);
    }
}
