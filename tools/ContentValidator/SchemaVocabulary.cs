using System.Text.Json;
using System.Text.RegularExpressions;

namespace PaleCommunion.Tools.ContentValidator;

/// <summary>
/// The fixed vocabularies read straight out of schemas/event.schema.json, so the
/// validator never drifts from the schema (law 5: the schema is the source of
/// truth for effect/condition types, attributes, skills, statuses, and so on).
/// </summary>
public sealed class SchemaVocabulary
{
    public required IReadOnlySet<string> Categories { get; init; }
    public required IReadOnlySet<string> EffectTypes { get; init; }
    public required IReadOnlySet<string> ConditionTypes { get; init; }
    public required IReadOnlySet<string> ConditionGroupTypes { get; init; }
    public required IReadOnlySet<string> Attributes { get; init; }
    public required IReadOnlySet<string> Skills { get; init; }
    public required IReadOnlySet<string> Ops { get; init; }
    public required IReadOnlySet<string> Statuses { get; init; }
    public required IReadOnlySet<string> Kinds { get; init; }
    public required IReadOnlySet<string> TriggerTypes { get; init; }
    public required IReadOnlySet<string> SelectorFrom { get; init; }
    public required IReadOnlySet<string> SelectorPrefer { get; init; }
    public required IReadOnlySet<string> RequirementModes { get; init; }
    public required Regex IdPattern { get; init; }

    public static SchemaVocabulary Load(string schemaPath)
    {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(schemaPath));
        JsonElement defs = doc.RootElement.GetProperty("$defs");

        HashSet<string> Enum(string def, string prop) =>
            defs.GetProperty(def).GetProperty("properties").GetProperty(prop)
                .GetProperty("enum").EnumerateArray()
                .Select(e => e.GetString()!)
                .ToHashSet(StringComparer.Ordinal);

        HashSet<string> groupTypes = Enum("conditionGroup", "type");
        var conditionTypes = new HashSet<string>(Enum("conditionLeaf", "type"), StringComparer.Ordinal);
        conditionTypes.UnionWith(groupTypes);

        string pattern = defs.GetProperty("event").GetProperty("properties")
            .GetProperty("id").GetProperty("pattern").GetString()!;

        return new SchemaVocabulary
        {
            Categories = Enum("event", "category"),
            EffectTypes = Enum("effect", "type"),
            ConditionTypes = conditionTypes,
            ConditionGroupTypes = groupTypes,
            Attributes = Enum("check", "attribute"),
            Skills = Enum("check", "skill"),
            Ops = Enum("conditionLeaf", "op"),
            Statuses = Enum("effect", "status"),
            Kinds = Enum("effect", "kind"),
            TriggerTypes = Enum("trigger", "type"),
            SelectorFrom = Enum("selector", "from"),
            SelectorPrefer = Enum("selector", "prefer"),
            RequirementModes = Enum("choice", "requirement_mode"),
            IdPattern = new Regex(pattern, RegexOptions.Compiled),
        };
    }
}
