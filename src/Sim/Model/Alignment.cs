namespace PaleCommunion.Sim.Model;

/// <summary>Where a leaning falls on the Law–Chaos axis.</summary>
public enum AlignmentPole
{
    Law,
    Neutral,
    Chaos,
}

/// <summary>
/// The Law(−)/Chaos(+) scale shared by members and the order. Values run −100..100;
/// the central band reads as Neutral. The bound and the band width are PLACEHOLDER
/// balance knobs (CLAUDE.md: tuning is a design decision) — revise freely.
/// </summary>
public static class AlignmentScale
{
    public const double Min = -100;
    public const double Max = 100;

    /// <summary>Half-width of the Neutral band around 0.</summary>
    public const double NeutralBand = 33;

    public static double Clamp(double value) => Math.Clamp(value, Min, Max);

    public static AlignmentPole Classify(double value) =>
        value < -NeutralBand ? AlignmentPole.Law
        : value > NeutralBand ? AlignmentPole.Chaos
        : AlignmentPole.Neutral;
}
