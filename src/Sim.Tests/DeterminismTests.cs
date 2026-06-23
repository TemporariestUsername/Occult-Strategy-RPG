using PaleCommunion.Sim.Determinism;

namespace PaleCommunion.Sim.Tests;

public class DeterminismTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var a = new SplitMix64Rng(123456);
        var b = new SplitMix64Rng(123456);
        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(a.NextUInt64(), b.NextUInt64());
        }
    }

    [Fact]
    public void DifferentSeeds_Diverge()
    {
        var a = new SplitMix64Rng(1);
        var b = new SplitMix64Rng(2);
        bool anyDifferent = false;
        for (int i = 0; i < 10; i++)
        {
            if (a.NextUInt64() != b.NextUInt64())
            {
                anyDifferent = true;
            }
        }

        Assert.True(anyDifferent);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 5)]
    [InlineData(1, 7)]
    public void NextInt_StaysWithinRange(int min, int max)
    {
        var rng = new SplitMix64Rng(99);
        for (int i = 0; i < 10000; i++)
        {
            Assert.InRange(rng.NextInt(min, max), min, max - 1);
        }
    }

    [Fact]
    public void NextInt_InvalidRange_Throws()
    {
        var rng = new SplitMix64Rng(0);
        Assert.Throws<ArgumentException>(() => rng.NextInt(5, 5));
    }

    [Fact]
    public void State_CanBeCapturedAndRestored()
    {
        var rng = new SplitMix64Rng(42);
        for (int i = 0; i < 5; i++)
        {
            rng.NextUInt64();
        }

        ulong saved = rng.State;
        ulong expectedNext = rng.NextUInt64();

        rng.State = saved;
        Assert.Equal(expectedNext, rng.NextUInt64());
    }

    [Fact]
    public void NextDouble_IsInUnitInterval()
    {
        var rng = new SplitMix64Rng(7);
        for (int i = 0; i < 10000; i++)
        {
            double d = rng.NextDouble();
            Assert.True(d is >= 0.0 and < 1.0, $"NextDouble out of range: {d}");
        }
    }
}
