using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;

namespace PaleCommunion.Sim.Tests;

public class CharacterFactoryTests
{
    [Fact]
    public void Create_IsDeterministic_ForSameSeed()
    {
        Member a = CharacterFactory.Create(new SplitMix64Rng(42), "x", "scholar");
        Member b = CharacterFactory.Create(new SplitMix64Rng(42), "x", "scholar");

        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.Attributes, b.Attributes);
        Assert.Equal(a.Skills, b.Skills);
        Assert.Equal(a.Traits, b.Traits);
    }

    [Fact]
    public void Create_GivesAName_AndAllFiveAttributesInRange()
    {
        Member m = CharacterFactory.Create(new SplitMix64Rng(7), "x");

        Assert.False(string.IsNullOrWhiteSpace(m.Name));
        Assert.Contains(' ', m.Name); // first + surname
        Assert.Equal(5, m.Attributes.Count);
        Assert.All(m.Attributes.Values, v => Assert.InRange(v, 1, 4));
    }

    [Fact]
    public void Create_BiasesArchetypeFocusSkills()
    {
        // The scholar archetype leans into Lore and Medicine; focus skills are 2..4.
        Member m = CharacterFactory.Create(new SplitMix64Rng(13), "x", "scholar");

        Assert.True(m.GetSkill("Lore") >= 2);
        Assert.True(m.GetSkill("Medicine") >= 2);
        Assert.All(m.Skills.Values, v => Assert.InRange(v, 1, 4));
    }

    [Fact]
    public void Create_UnknownArchetype_StillProducesAValidInitiate()
    {
        Member m = CharacterFactory.Create(new SplitMix64Rng(99), "x", "no_such_archetype");

        Assert.Equal(MemberStatus.Active, m.Status);
        Assert.Equal(5, m.Attributes.Count);
        Assert.NotEmpty(m.Skills);
    }
}
