using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;

namespace PaleCommunion.Sim.Engine;

/// <summary>
/// Builds initiates deterministically from the seeded RNG (law 3): a name, the five
/// attributes, a spread of skills, and a trait or two, biased by an optional archetype.
///
/// Everything here — the name pools, the stat ranges, the archetype table — is a
/// tunable PLACEHOLDER. Distribution, balance, and the writing of names are design /
/// voice decisions (CLAUDE.md division of labor); revise freely. Archetypes living in
/// code is a stopgap until character templates become authored data.
/// </summary>
public static class CharacterFactory
{
    public static readonly IReadOnlyList<string> AttributeNames =
        new[] { "Intellect", "Will", "Presence", "Guile", "Body" };

    public static readonly IReadOnlyList<string> SkillNames =
        new[] { "Lore", "Ritual", "Infiltration", "Persuasion", "Violence", "Medicine", "Finance" };

    // Period-flavoured Boston, c. 1905 — a mix of Brahmin and immigrant names. Placeholder.
    private static readonly string[] FirstNames =
    {
        "Cornelius", "Edith", "Aloysius", "Florence", "Ezra", "Augusta", "Silas", "Beatrice",
        "Declan", "Winifred", "Horace", "Cordelia", "Ambrose", "Mabel", "Thaddeus", "Lavinia",
        "Barnabas", "Prudence", "Lucius", "Harriet", "Mercy", "Adeline", "Octavia", "Roland",
    };

    private static readonly string[] Surnames =
    {
        "Cabot", "Donnelly", "Pike", "Ashe", "Calloway", "Sturgis", "Merrow", "Halloran",
        "Vane", "Endicott", "Crane", "Thorne", "Brennan", "Lowell", "Sable", "Quill",
        "Marsh", "Pruitt", "Goss", "Wexford", "Coffin", "Devereux",
    };

    private sealed record Archetype(string[] Skills, string Attribute, string[] Traits);

    private static readonly IReadOnlyDictionary<string, Archetype> Archetypes =
        new Dictionary<string, Archetype>(StringComparer.Ordinal)
        {
            ["scholar"] = new(new[] { "Lore", "Medicine" }, "Intellect", new[] { "scholar", "skeptic" }),
            ["occultist"] = new(new[] { "Ritual", "Lore" }, "Will", new[] { "touched", "devout" }),
            ["street_tough"] = new(new[] { "Infiltration", "Violence" }, "Body", new[] { "veteran", "addict" }),
            ["society_patron"] = new(new[] { "Persuasion", "Finance" }, "Presence", new[] { "aristocrat", "persuasive" }),
            ["lapsed_cleric"] = new(new[] { "Persuasion", "Medicine" }, "Will", new[] { "devout", "zealot" }),
            ["state_official"] = new(new[] { "Finance", "Infiltration" }, "Guile", new[] { "aristocrat", "persuasive" }),
            ["stray_seeker"] = new(new[] { "Infiltration", "Lore" }, "Guile", new[] { "ambitious" }),
            ["curious_soul"] = new(new[] { "Lore", "Persuasion" }, "Presence", new[] { "touched" }),
        };

    private static readonly string[] AllTraits =
    {
        "ambitious", "zealot", "skeptic", "touched", "scholar", "aristocrat",
        "veteran", "addict", "devout", "persuasive",
    };

    /// <summary>Create one initiate with the given id, optionally biased by a named archetype.</summary>
    public static Member Create(IRng rng, string id, string? archetype = null)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentException.ThrowIfNullOrEmpty(id);

        Archetypes.TryGetValue(archetype ?? string.Empty, out Archetype? arch);

        var member = new Member { Id = id, Name = RandomName(rng), Status = MemberStatus.Active };

        foreach (string attribute in AttributeNames)
        {
            int value = 1 + rng.NextInt(0, 3); // 1..3
            if (arch is not null && arch.Attribute == attribute)
            {
                value += 1; // a leaning, not a guarantee — 2..4
            }

            member.Attributes[attribute] = value;
        }

        string[] focus = arch?.Skills ?? new[] { RandomSkill(rng), RandomSkill(rng) };
        foreach (string skill in focus)
        {
            member.Skills[skill] = Math.Max(member.GetSkill(skill), 2 + rng.NextInt(0, 3)); // 2..4
        }

        // One incidental skill, so two initiates of the same archetype still differ.
        string incidental = RandomSkill(rng);
        if (!member.Skills.ContainsKey(incidental))
        {
            member.Skills[incidental] = 1 + rng.NextInt(0, 2); // 1..2
        }

        if (arch is not null && arch.Traits.Length > 0 && rng.Chance(0.6))
        {
            member.Traits.Add(arch.Traits[rng.NextInt(0, arch.Traits.Length)]);
        }

        if (rng.Chance(0.25))
        {
            member.Traits.Add(AllTraits[rng.NextInt(0, AllTraits.Length)]);
        }

        return member;
    }

    private static string RandomName(IRng rng) =>
        $"{FirstNames[rng.NextInt(0, FirstNames.Length)]} {Surnames[rng.NextInt(0, Surnames.Length)]}";

    private static string RandomSkill(IRng rng) => SkillNames[rng.NextInt(0, SkillNames.Count)];
}
