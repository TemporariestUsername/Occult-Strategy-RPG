using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class EffectHandlerTests
{
    private static IRng Rng() => new SplitMix64Rng(0);

    private static List<Effect> Effects(string json) =>
        JsonSerializer.Deserialize<List<Effect>>(json, SimJson.Content)!;

    private static (GameState State, BindingContext Context) WithActor()
    {
        var s = GameState.NewCampaign(1);
        var actor = new Member { Id = "actor", Name = "Actor", Status = MemberStatus.Active };
        s.Members.Add(actor);
        var ctx = new BindingContext();
        ctx.Set("actor", actor);
        return (s, ctx);
    }

    [Fact]
    public void AddTrait_MemberStatus_Kill()
    {
        (GameState s, BindingContext ctx) = WithActor();

        EffectEngine.Apply(s, Effects("[{\"type\":\"add_trait\",\"scope\":\"actor\",\"id\":\"touched\"}]"), Rng(), ctx);
        Assert.True(s.Members[0].HasTrait("touched"));

        EffectEngine.Apply(s, Effects("[{\"type\":\"member_status\",\"scope\":\"actor\",\"status\":\"injured\"}]"), Rng(), ctx);
        Assert.Equal(MemberStatus.Injured, s.Members[0].Status);

        EffectEngine.Apply(s, Effects("[{\"type\":\"kill_member\",\"scope\":\"actor\"}]"), Rng(), ctx);
        Assert.Equal(MemberStatus.Dead, s.Members[0].Status);
        Assert.False(s.Members[0].IsAlive);
    }

    [Fact]
    public void MemberCorruption_Clamps()
    {
        (GameState s, BindingContext ctx) = WithActor();
        EffectEngine.Apply(s, Effects("[{\"type\":\"member_corruption\",\"scope\":\"actor\",\"amount\":500}]"), Rng(), ctx);
        Assert.Equal(100, s.Members[0].Corruption);
    }

    [Fact]
    public void Recruit_AddsMembers_Deterministically()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"recruit\",\"template\":\"state_official\",\"count\":2}]"), Rng());

        Assert.Equal(2, s.Members.Count);
        Assert.Equal("state_official_1", s.Members[0].Id);
        Assert.Equal("state_official_2", s.Members[1].Id);
    }

    [Fact]
    public void Secrets_GainAndReveal()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"gain_secret\",\"id\":\"magistrate_second_name\",\"about_scope\":\"actor\"}]"), Rng());
        Assert.Contains("magistrate_second_name", s.KnownSecrets);

        EffectEngine.Apply(s, Effects("[{\"type\":\"reveal_secret\",\"id\":\"magistrate_second_name\"}]"), Rng());
        Assert.DoesNotContain("magistrate_second_name", s.KnownSecrets);
    }

    [Fact]
    public void Reagents_GrantAndRemove_ClampToRemoval()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"grant_reagent\",\"id\":\"graveyard_dirt\",\"count\":3}]"), Rng());
        Assert.Equal(3, s.ReagentItems["graveyard_dirt"]);

        EffectEngine.Apply(s, Effects("[{\"type\":\"remove_reagent\",\"id\":\"graveyard_dirt\",\"count\":10}]"), Rng());
        Assert.False(s.ReagentItems.ContainsKey("graveyard_dirt"));
    }

    [Fact]
    public void Relationship_AccumulatesBetweenScopes()
    {
        var s = GameState.NewCampaign(1);
        var a = new Member { Id = "a" };
        var b = new Member { Id = "b" };
        s.Members.Add(a);
        s.Members.Add(b);
        var ctx = new BindingContext();
        ctx.Set("actor", a);
        ctx.Set("rival", b);

        EffectEngine.Apply(s, Effects("[{\"type\":\"relationship\",\"scope\":\"actor\",\"target_scope\":\"rival\",\"kind\":\"rivalry\",\"amount\":10}]"), Rng(), ctx);
        EffectEngine.Apply(s, Effects("[{\"type\":\"relationship\",\"scope\":\"actor\",\"target_scope\":\"rival\",\"kind\":\"rivalry\",\"amount\":5}]"), Rng(), ctx);

        Assert.Single(s.Relationships);
        Assert.Equal(15, s.Relationships[0].Value);
        Assert.Equal(RelationshipKind.Rivalry, s.Relationships[0].Kind);
    }

    [Fact]
    public void Patron_Unlock_Tier_EndGame()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects(
            "[{\"type\":\"patron_relationship\",\"id\":\"the_listener\",\"amount\":20}," +
            "{\"type\":\"unlock_ritual\",\"id\":\"the_borrowed_name\"}," +
            "{\"type\":\"tier_change\",\"amount\":1}," +
            "{\"type\":\"end_game\",\"ending\":\"the_opening\"}]"), Rng());

        Assert.Equal(20, s.PatronRelationships["the_listener"]);
        Assert.Contains("the_borrowed_name", s.UnlockedRituals);
        Assert.Equal(1, s.Tier);
        Assert.True(s.IsGameOver);
        Assert.Equal("the_opening", s.Ending);
    }

    [Fact]
    public void GreatWorkEffects_StillDeferred()
    {
        var s = GameState.NewCampaign(1);
        EffectResult r = EffectEngine.Apply(s, Effects("[{\"type\":\"great_work_advance\",\"id\":\"the_opening\",\"steps\":1}]"), Rng());
        Assert.Contains("great_work_advance", r.Unhandled);
        Assert.False(r.FullyApplied);
    }

    [Fact]
    public void ScopedEffect_WithoutBinding_ThrowsLoudly()
    {
        var s = GameState.NewCampaign(1);
        Assert.Throws<InvalidOperationException>(
            () => EffectEngine.Apply(s, Effects("[{\"type\":\"add_trait\",\"scope\":\"actor\",\"id\":\"touched\"}]"), Rng(), null));
    }
}
