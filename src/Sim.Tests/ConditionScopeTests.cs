using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class ConditionScopeTests
{
    private static IRng Rng() => new SplitMix64Rng(0);

    private static Condition Cond(string json) => JsonSerializer.Deserialize<Condition>(json, SimJson.Content)!;

    private static BindingContext Actor(Member m)
    {
        var c = new BindingContext();
        c.Set("actor", m);
        return c;
    }

    [Fact]
    public void ScopeHasTrait_UsesBoundMember()
    {
        var s = GameState.NewCampaign(1);
        var actor = new Member { Id = "a" };
        actor.Traits.Add("aristocrat");
        BindingContext ctx = Actor(actor);

        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"scope_has_trait\",\"scope\":\"actor\",\"id\":\"aristocrat\"}"), Rng(), ctx));
        Assert.False(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"scope_has_trait\",\"scope\":\"actor\",\"id\":\"zealot\"}"), Rng(), ctx));
    }

    [Fact]
    public void ScopeSkill_PresenceAndComparison()
    {
        var s = GameState.NewCampaign(1);
        var actor = new Member { Id = "a" };
        actor.Skills["Lore"] = 3;
        BindingContext ctx = Actor(actor);

        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"scope_skill\",\"scope\":\"actor\",\"key\":\"Lore\"}"), Rng(), ctx));
        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"scope_skill\",\"scope\":\"actor\",\"key\":\"Lore\",\"op\":\"gte\",\"value\":3}"), Rng(), ctx));
        Assert.False(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"scope_skill\",\"scope\":\"actor\",\"key\":\"Lore\",\"op\":\"gte\",\"value\":4}"), Rng(), ctx));
    }

    [Fact]
    public void Secret_Patron_Relic_ReadState()
    {
        var s = GameState.NewCampaign(1);
        s.KnownSecrets.Add("rival_true_name");
        s.PatronRelationships["the_listener"] = 20;
        s.Relics.Add("the_black_key");

        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"secret_known\",\"id\":\"rival_true_name\"}"), Rng()));
        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"patron_relationship\",\"id\":\"the_listener\",\"op\":\"gte\",\"value\":10}"), Rng()));
        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"has_relic\",\"id\":\"the_black_key\"}"), Rng()));
        Assert.False(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"has_relic\",\"id\":\"nope\"}"), Rng()));
    }

    [Fact]
    public void MemberWithTrait_ScansLivingRoster()
    {
        var s = GameState.NewCampaign(1);
        var touched = new Member { Id = "a" };
        touched.Traits.Add("touched");
        s.Members.Add(touched);

        var deadDevout = new Member { Id = "b", Status = MemberStatus.Dead };
        deadDevout.Traits.Add("devout");
        s.Members.Add(deadDevout);

        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"member_with_trait\",\"id\":\"touched\"}"), Rng()));
        Assert.False(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"member_with_trait\",\"id\":\"devout\"}"), Rng())); // only on a dead member
    }

    [Fact]
    public void UnboundScope_ThrowsLoudly()
    {
        var s = GameState.NewCampaign(1);
        Assert.Throws<InvalidOperationException>(
            () => ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"scope_has_trait\",\"scope\":\"actor\",\"id\":\"x\"}"), Rng(), null));
    }
}
