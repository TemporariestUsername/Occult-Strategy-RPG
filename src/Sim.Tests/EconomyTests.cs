using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class EconomyTests
{
    private static IRng Rng() => new SplitMix64Rng(0);

    private static List<Effect> Effects(string json) =>
        JsonSerializer.Deserialize<List<Effect>>(json, SimJson.Content)!;

    private static Condition Cond(string json) =>
        JsonSerializer.Deserialize<Condition>(json, SimJson.Content)!;

    [Fact]
    public void Resource_Effect_AddsLore()
    {
        var s = GameState.NewCampaign(1);
        EffectResult r = EffectEngine.Apply(s, Effects("[{\"type\":\"resource\",\"key\":\"lore\",\"amount\":5}]"), Rng());
        Assert.Equal(5, s.Lore);
        Assert.True(r.FullyApplied);
    }

    [Fact]
    public void Resource_CannotGoNegative()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"resource\",\"key\":\"funds\",\"amount\":-99999}]"), Rng());
        Assert.Equal(0, s.Funds);
    }

    [Fact]
    public void Veil_ClampsToHundred()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"veil\",\"amount\":1000}]"), Rng());
        Assert.Equal(100, s.Veil);
    }

    [Fact]
    public void InstitutionInfluence_Increases()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"institution_influence\",\"key\":\"state\",\"amount\":15}]"), Rng());
        Assert.Equal(15, s.Institutions["state"].Influence);
    }

    [Fact]
    public void SetFlag_ThenConditionReadsTrue()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"set_flag\",\"id\":\"order_named\",\"value\":true}]"), Rng());
        Assert.True(s.Flags["order_named"]);
        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"flag\",\"id\":\"order_named\",\"value\":true}"), Rng()));
    }

    [Fact]
    public void DeferredEffects_AreReportedNotApplied()
    {
        var s = GameState.NewCampaign(1);

        // The on_success effects of intrigue.magistrate_weakness from example_events.json:
        // the economy effects apply now; gain_secret is part of the vocabulary but its
        // handler ships with the secrets system.
        string json = "[" +
            "{\"type\":\"institution_influence\",\"key\":\"state\",\"amount\":15}," +
            "{\"type\":\"gain_secret\",\"id\":\"magistrate_second_name\",\"about_scope\":\"actor\"}," +
            "{\"type\":\"attention\",\"key\":\"mundane\",\"amount\":5}]";
        EffectResult r = EffectEngine.Apply(s, Effects(json), Rng());

        Assert.Equal(15, s.Institutions["state"].Influence);
        Assert.Equal(5, s.AttentionMundane);
        Assert.Contains("gain_secret", r.Unhandled);
        Assert.False(r.FullyApplied);
    }

    [Theory]
    [InlineData("gte", 20, true)]   // Veil starts at 80
    [InlineData("lte", 20, false)]
    [InlineData("lt", 81, true)]
    [InlineData("eq", 80, true)]
    public void VeilComparisons(string op, double value, bool expected)
    {
        var s = GameState.NewCampaign(1);
        Assert.Equal(expected, ConditionEvaluator.Evaluate(s, Cond($"{{\"type\":\"veil\",\"op\":\"{op}\",\"value\":{value}}}"), Rng()));
    }

    [Fact]
    public void MemberCount_CountsLivingMembersOnly()
    {
        var s = GameState.NewCampaign(1);
        s.Members.Add(new Member { Id = "a", Name = "A", Status = MemberStatus.Active });
        s.Members.Add(new Member { Id = "b", Name = "B", Status = MemberStatus.Dead });

        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"member_count\",\"op\":\"gte\",\"value\":1}"), Rng()));
        Assert.False(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"member_count\",\"op\":\"gte\",\"value\":2}"), Rng()));
    }

    [Fact]
    public void NestedConditionTree_Evaluates()
    {
        var s = GameState.NewCampaign(1); // Veil 80, no members
        s.Members.Add(new Member { Id = "a", Name = "A" });

        string json = "{\"type\":\"all_of\",\"conditions\":[" +
            "{\"type\":\"member_count\",\"op\":\"gte\",\"value\":1}," +
            "{\"type\":\"veil\",\"op\":\"gte\",\"value\":20}]}";
        Assert.True(ConditionEvaluator.Evaluate(s, Cond(json), Rng()));
    }

    [Fact]
    public void UnsupportedConditionLeaf_ThrowsLoudly()
    {
        var s = GameState.NewCampaign(1);
        Assert.Throws<NotSupportedException>(
            () => ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"scope_has_trait\",\"scope\":\"actor\",\"id\":\"zealot\"}"), Rng()));
    }
}
