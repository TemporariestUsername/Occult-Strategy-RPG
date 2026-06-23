using System.Text.Json;

namespace PaleCommunion.Tools.ContentValidator;

/// <summary>
/// The content registry (content/registry.json): the set of valid ids for each
/// open content category (traits, rituals, patrons, secrets, …) plus the fixed
/// canonical institutions/resources/attention channels. References in events are
/// checked against this so a typo'd id is caught at validation time.
/// </summary>
public sealed class ContentRegistry
{
    private static readonly HashSet<string> EmptySet = new();

    private readonly Dictionary<string, HashSet<string>> _sets;

    private ContentRegistry(Dictionary<string, HashSet<string>> sets) => _sets = sets;

    public IReadOnlySet<string> Get(string category) =>
        _sets.TryGetValue(category, out HashSet<string>? set) ? set : EmptySet;

    public bool Has(string category, string id) => Get(category).Contains(id);

    public bool KnowsCategory(string category) => _sets.ContainsKey(category);

    public static ContentRegistry Load(string path)
    {
        var sets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Array)
            {
                continue; // skip _comment and any non-array metadata
            }

            sets[prop.Name] = prop.Value.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .ToHashSet(StringComparer.Ordinal);
        }

        return new ContentRegistry(sets);
    }
}
