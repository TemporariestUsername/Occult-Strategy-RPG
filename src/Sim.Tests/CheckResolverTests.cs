using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class CheckResolverTests
{
    private static Check Parse(string json) => JsonSerializer.Deserialize<Check>(json, SimJson.Content)!;

    private static BindingContext WithActor(Member actor)
    {
        var ctx = new BindingContext();
        ctx.Set("actor", actor);
        return ctx;
    }

    private static Member Actor(int guile, int infiltration)
    {
        var m = new Member { Id = "actor", Name = "actor" };
        m.Attributes["Guile"] = guile;
        m.Skills["Infiltration"] = infiltration;
        return m;
    }

    [Fact]
    public void SameSeed_SameInputs_IsDeterministic()
    {
        var s = GameState.NewCampaign(1);
        var ctx = WithActor(Actor(3, 2));
        Check check = Parse("{\"scope\":\"actor\",\"attribute\":\"Guile\",\"skill\":\"Infiltration\",\"difficulty\":12}");

        CheckResult r1 = CheckResolver.Resolve(s, check, ctx, new SplitMix64Rng(99));
        CheckResult r2 = CheckResolver.Resolve(s, check, ctx, new SplitMix64Rng(99));

        Assert.Equal(r1.DieA, r2.DieA);
        Assert.Equal(r1.DieB, r2.DieB);
        Assert.Equal(r1.Total, r2.Total);
        Assert.Equal(r1.Outcome, r2.Outcome);
    }

    [Fact]
    public void Total_Equals_Dice_Plus_Stats_Plus_Modifiers()
    {
        var s = GameState.NewCampaign(1);
        var ctx = WithActor(Actor(3, 2)); // stat bonus 5
        Check check = Parse("{\"scope\":\"actor\",\"attribute\":\"Guile\",\"skill\":\"Infiltration\",\"difficulty\":1}");

        CheckResult r = CheckResolver.Resolve(s, check, ctx, new SplitMix64Rng(7));

        Assert.Equal(5, r.StatBonus);
        Assert.Equal(r.DieA + r.DieB + r.StatBonus + r.ModifierBonus, r.Total);
        Assert.InRange(r.DieA, 1, 6);
        Assert.InRange(r.DieB, 1, 6);
    }

    [Fact]
    public void Modifier_Applies_OnlyWhenConditionHolds()
    {
        var s = GameState.NewCampaign(1);
        Check check = Parse("{\"scope\":\"actor\",\"attribute\":\"Guile\",\"difficulty\":12,\"modifiers\":[{\"value\":3,\"when\":{\"type\":\"scope_has_trait\",\"scope\":\"actor\",\"id\":\"aristocrat\"}}]}");

        var withTrait = Actor(3, 0);
        withTrait.Traits.Add("aristocrat");

        CheckResult a = CheckResolver.Resolve(s, check, WithActor(withTrait), new SplitMix64Rng(3));
        CheckResult b = CheckResolver.Resolve(s, check, WithActor(Actor(3, 0)), new SplitMix64Rng(3));

        Assert.Equal(3, a.ModifierBonus);
        Assert.Equal(0, b.ModifierBonus);
    }

    [Fact]
    public void NaturalRolls_AreCriticalOutcomes()
    {
        var s = GameState.NewCampaign(1);
        var ctx = WithActor(Actor(0, 0));
        Check check = Parse("{\"scope\":\"actor\",\"attribute\":\"Guile\",\"difficulty\":7}");

        bool sawBoxcars = false;
        bool sawSnakeEyes = false;
        for (ulong seed = 0; seed < 4000 && !(sawBoxcars && sawSnakeEyes); seed++)
        {
            CheckResult r = CheckResolver.Resolve(s, check, ctx, new SplitMix64Rng(seed));
            if (r.DieA == 6 && r.DieB == 6)
            {
                Assert.Equal(CheckOutcome.CriticalSuccess, r.Outcome);
                sawBoxcars = true;
            }

            if (r.DieA == 1 && r.DieB == 1)
            {
                Assert.Equal(CheckOutcome.CriticalFailure, r.Outcome);
                sawSnakeEyes = true;
            }
        }

        Assert.True(sawBoxcars && sawSnakeEyes, "expected to observe both 6,6 and 1,1 across seeds");
    }

    [Fact]
    public void UnboundCheckScope_ThrowsLoudly()
    {
        var s = GameState.NewCampaign(1);
        Check check = Parse("{\"scope\":\"actor\",\"skill\":\"Lore\",\"difficulty\":10}");
        Assert.Throws<InvalidOperationException>(() => CheckResolver.Resolve(s, check, null, new SplitMix64Rng(1)));
    }
}
