using System.Text.Json;
using PaleCommunion.Sim.Engine;

namespace PaleCommunion.Sim.Tests;

/// <summary>
/// Guards architecture law 5: an effect <c>type</c> in the schema must have a handler in
/// the simulation. If this fails, a type was added to schemas/event.schema.json without
/// a matching handler in EffectEngine (or vice versa).
/// </summary>
public class SchemaCoverageTests
{
    [Fact]
    public void EverySchemaEffectType_HasAHandler()
    {
        using JsonDocument doc = JsonDocument.Parse(
            File.ReadAllText(TestPaths.RepoFile(Path.Combine("schemas", "event.schema.json"))));

        List<string> effectTypes = doc.RootElement
            .GetProperty("$defs").GetProperty("effect").GetProperty("properties")
            .GetProperty("type").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString()!).ToList();

        IReadOnlyCollection<string> handled = EffectEngine.HandledTypes;
        List<string> missing = effectTypes.Where(t => !handled.Contains(t)).ToList();

        Assert.True(missing.Count == 0, $"Schema effect types with no EffectEngine handler: {string.Join(", ", missing)}");
    }
}
