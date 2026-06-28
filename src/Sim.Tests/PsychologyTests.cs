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
}
