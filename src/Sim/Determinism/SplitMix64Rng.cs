namespace PaleCommunion.Sim.Determinism;

/// <summary>
/// SplitMix64 — a small, fully specified, platform-stable PRNG. Chosen over
/// System.Random because the simulation must be reproducible from a seed across
/// machines and .NET versions (law 3). State is a single ulong, so it round-trips
/// trivially in saves (law 4).
/// </summary>
public sealed class SplitMix64Rng : IRng
{
    private ulong _state;

    public SplitMix64Rng(ulong seed) => _state = seed;

    public ulong State
    {
        get => _state;
        set => _state = value;
    }

    public ulong NextUInt64()
    {
        unchecked
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    public double NextDouble()
    {
        // Top 53 bits give a uniform double in [0, 1). 2^53 = 9007199254740992.
        return (NextUInt64() >> 11) * (1.0 / 9007199254740992.0);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentException(
                $"maxExclusive ({maxExclusive}) must be greater than minInclusive ({minInclusive}).");
        }

        ulong range = (ulong)((long)maxExclusive - minInclusive);

        // Reject the short tail so every value in the range is equally likely.
        ulong threshold = unchecked(0UL - range) % range;
        ulong r;
        do
        {
            r = NextUInt64();
        }
        while (r < threshold);

        return (int)((long)minInclusive + (long)(r % range));
    }

    public bool Chance(double probability)
    {
        if (probability <= 0.0)
        {
            return false;
        }
        if (probability >= 1.0)
        {
            return true;
        }
        return NextDouble() < probability;
    }
}
