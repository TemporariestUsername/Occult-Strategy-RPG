using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class PsychologyTests
{
    [Fact]
    public void Member_DefaultsToSoundMindAndNeutral()
    {
        var m = new Member { Id = "a" };
        Assert.Equal(100, m.Sanity);
        Assert.Equal(0, m.Alignment);
        Assert.Equal(AlignmentPole.Neutral, m.Leaning);
    }

    [Theory]
    [InlineData(-100, AlignmentPole.Law)]
    [InlineData(-40, AlignmentPole.Law)]
    [InlineData(0, AlignmentPole.Neutral)]
    [InlineData(20, AlignmentPole.Neutral)]
    [InlineData(80, AlignmentPole.Chaos)]
    public void AlignmentScale_ClassifiesByBand(double value, AlignmentPole expected)
    {
        Assert.Equal(expected, AlignmentScale.Classify(value));
    }

    [Fact]
    public void Normalize_ClampsSanityAndAlignment()
    {
        var s = GameState.NewCampaign(1);
        s.Alignment = 999;
        var m = new Member { Id = "a", Sanity = 250, Alignment = -250 };
        s.Members.Add(m);

        s.Normalize();

        Assert.Equal(100, s.Alignment);
        Assert.Equal(100, m.Sanity);
        Assert.Equal(-100, m.Alignment);
    }

    [Fact]
    public void AdjustRelationship_CreatesThenAccumulatesAndClamps()
    {
        var s = GameState.NewCampaign(1);

        s.AdjustRelationship("a", "b", RelationshipKind.Loyalty, 30);
        Assert.Equal(30, s.FindRelationship("a", "b", RelationshipKind.Loyalty)!.Value);

        s.AdjustRelationship("a", "b", RelationshipKind.Loyalty, 90);
        Assert.Equal(100, s.FindRelationship("a", "b", RelationshipKind.Loyalty)!.Value); // clamped
    }

    [Fact]
    public void NetOpinion_RivalryCountsAgainstAffection()
    {
        var s = GameState.NewCampaign(1);
        s.AdjustRelationship("a", "b", RelationshipKind.Loyalty, 40);
        s.AdjustRelationship("a", "b", RelationshipKind.Rivalry, 25);

        Assert.Equal(15, s.NetOpinion("a", "b"));
    }

    [Fact]
    public void ResolveBreaks_LeavesSoundMembersAlone()
    {
        var s = GameState.NewCampaign(1);
        var m = new Member { Id = "a", Sanity = 50, Status = MemberStatus.Active };
        s.Members.Add(m);

        IReadOnlyList<SanityBreak> breaks = SanitySystem.ResolveBreaks(s, new SplitMix64Rng(s.RngState));

        Assert.Empty(breaks);
        Assert.Equal(MemberStatus.Active, m.Status);
    }

    [Fact]
    public void ResolveBreaks_AtFloor_IsDeterministicForASeed()
    {
        SanityBreakKind Run()
        {
            var s = GameState.NewCampaign(1);
            s.Members.Add(new Member { Id = "a", Sanity = 0, Status = MemberStatus.Active });
            return Assert.Single(SanitySystem.ResolveBreaks(s, new SplitMix64Rng(123))).Kind;
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void ResolveBreaks_OverACohort_YieldsValidStatusesAndNeverRaisesDevotion()
    {
        var s = GameState.NewCampaign(7);
        for (int i = 0; i < 40; i++)
        {
            s.Members.Add(new Member { Id = $"m{i}", Sanity = 0, Status = MemberStatus.Active });
        }

        IReadOnlyList<SanityBreak> breaks = SanitySystem.ResolveBreaks(s, new SplitMix64Rng(7));

        Assert.Equal(40, breaks.Count);
        MemberStatus[] valid = { MemberStatus.Active, MemberStatus.Maddened, MemberStatus.Missing, MemberStatus.Dead };
        Assert.All(s.Members, m => Assert.Contains(m.Status, valid));
        Assert.True(s.Devotion <= 60); // lash-outs can only lower cohesion
    }

    [Fact]
    public void MemberSanityEffect_Broadcast_DrainsEveryLivingMember()
    {
        var s = GameState.NewCampaign(1);
        var a = new Member { Id = "a", Sanity = 80, Status = MemberStatus.Active };
        var b = new Member { Id = "b", Sanity = 40, Status = MemberStatus.Active };
        var dead = new Member { Id = "c", Sanity = 90, Status = MemberStatus.Dead };
        s.Members.AddRange(new[] { a, b, dead });

        EffectEngine.Apply(s, new[] { new Effect { Type = "member_sanity", Amount = -25 } }, new SplitMix64Rng(1));

        Assert.Equal(55, a.Sanity);
        Assert.Equal(15, b.Sanity);
        Assert.Equal(90, dead.Sanity); // the dead are spared
    }

    [Fact]
    public void MemberSanityEffect_Scoped_DrainsOnlyTheBoundMember()
    {
        var s = GameState.NewCampaign(1);
        var a = new Member { Id = "a", Sanity = 50, Status = MemberStatus.Active };
        var b = new Member { Id = "b", Sanity = 50, Status = MemberStatus.Active };
        s.Members.AddRange(new[] { a, b });
        var ctx = new BindingContext();
        ctx.Set("actor", a);

        EffectEngine.Apply(
            s, new[] { new Effect { Type = "member_sanity", Scope = "actor", Amount = -30 } }, new SplitMix64Rng(1), ctx);

        Assert.Equal(20, a.Sanity);
        Assert.Equal(50, b.Sanity);
    }

    [Fact]
    public void OrderAlignmentEffect_ShiftsAndReadsBackAsACondition()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, new[] { new Effect { Type = "order_alignment", Amount = 45 } }, new SplitMix64Rng(1));

        Assert.Equal(45, s.Alignment);
        Assert.Equal(AlignmentPole.Chaos, AlignmentScale.Classify(s.Alignment));

        var leaningChaos = new Condition { Type = "order_alignment", Op = "gte", Value = JsonSerializer.SerializeToElement(33) };
        Assert.True(ConditionEvaluator.Evaluate(s, leaningChaos, new SplitMix64Rng(1)));
    }

    [Fact]
    public void AdvanceTurn_ResolvesSanityBreaksForFlooredMinds()
    {
        var s = GameState.NewCampaign(1);
        s.Members.Add(new Member { Id = "a", Sanity = 0, Status = MemberStatus.Active });

        TurnReport report = TurnSystem.Advance(s, new Dictionary<string, Scheme>(), new SplitMix64Rng(99));

        SanityBreak br = Assert.Single(report.SanityBreaks);
        Assert.Equal("a", br.MemberId);
    }
}
