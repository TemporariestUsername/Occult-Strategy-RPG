namespace PaleCommunion.Sim.Determinism;

/// <summary>
/// Deterministic random source. ALL randomness in the simulation flows through
/// this service (CLAUDE.md architecture law 3); never call System.Random or any
/// engine RNG directly.
/// </summary>
public interface IRng
{
    /// <summary>Raw 64-bit draw. Advances the stream.</summary>
    ulong NextUInt64();

    /// <summary>Uniform double in [0, 1).</summary>
    double NextDouble();

    /// <summary>Uniform integer in [minInclusive, maxExclusive). Unbiased.</summary>
    int NextInt(int minInclusive, int maxExclusive);

    /// <summary>True with probability <paramref name="probability"/> (clamped to [0, 1]).</summary>
    bool Chance(double probability);

    /// <summary>Current stream position, so it can be persisted in a save (law 4).</summary>
    ulong State { get; set; }
}
