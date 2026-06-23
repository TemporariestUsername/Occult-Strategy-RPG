namespace PaleCommunion.Sim.Model;

/// <summary>
/// Canonical content-id vocabularies the simulation treats as fixed. Institutions
/// and resources are free strings in content data (validated at load time against
/// the content registry); these constants are how the engine maps a content `key`
/// onto concrete state.
/// </summary>
public static class ContentIds
{
    // Institutions — the six-faction board (Boston, 1905–1925).
    public const string Church = "church";
    public const string University = "university";
    public const string Press = "press";
    public const string Underworld = "underworld";
    public const string State = "state";
    public const string HighSociety = "high_society";

    public static readonly IReadOnlyList<string> Institutions = new[]
    {
        Church, University, Press, Underworld, State, HighSociety,
    };

    // Bulk resources.
    public const string Funds = "funds";
    public const string Lore = "lore";
    public const string Reagents = "reagents";

    public static readonly IReadOnlyList<string> Resources = new[] { Funds, Lore, Reagents };

    // Attention channels.
    public const string AttentionMundane = "mundane";
    public const string AttentionOccult = "occult";

    public static readonly IReadOnlyList<string> AttentionChannels = new[]
    {
        AttentionMundane, AttentionOccult,
    };
}
