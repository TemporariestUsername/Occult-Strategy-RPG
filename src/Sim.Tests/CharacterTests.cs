using PaleCommunion.Sim.Model;

namespace PaleCommunion.Sim.Tests;

public class CharacterTests
{
    [Fact]
    public void Stats_DefaultToZero_AndReadBack()
    {
        var m = new Member { Id = "a", Name = "Adept" };
        Assert.Equal(0, m.GetAttribute("Guile"));
        Assert.Equal(0, m.GetSkill("Infiltration"));

        m.Attributes["Guile"] = 4;
        m.Skills["Infiltration"] = 3;

        Assert.Equal(4, m.GetAttribute("Guile"));
        Assert.Equal(3, m.GetSkill("Infiltration"));
        Assert.Equal(4, m.GetStat("Guile"));        // attribute wins
        Assert.Equal(3, m.GetStat("Infiltration"));  // then skill
        Assert.Equal(0, m.GetStat("Nonexistent"));
    }

    [Fact]
    public void Traits_RoundTrip()
    {
        var m = new Member { Id = "a" };
        Assert.False(m.HasTrait("aristocrat"));
        m.Traits.Add("aristocrat");
        Assert.True(m.HasTrait("aristocrat"));
    }

    [Fact]
    public void IsAlive_TracksStatus()
    {
        var m = new Member { Id = "a", Status = MemberStatus.Maddened };
        Assert.True(m.IsAlive);
        m.Status = MemberStatus.Dead;
        Assert.False(m.IsAlive);
    }
}
