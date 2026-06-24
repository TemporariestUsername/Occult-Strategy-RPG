using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Serialization;

namespace PaleCommunion.Game;

/// <summary>
/// Loads authored content (events, schemes, rituals) from a content directory into the
/// Sim content models. This is host/IO glue at the edge — it holds no game rules.
/// Packaging content into an exported build is a separate, later task.
/// </summary>
public sealed class ContentCatalog
{
    public IReadOnlyList<EventCard> Events { get; }
    public IReadOnlyDictionary<string, Scheme> Schemes { get; }
    public IReadOnlyDictionary<string, Ritual> Rituals { get; }

    private ContentCatalog(
        IReadOnlyList<EventCard> events,
        IReadOnlyDictionary<string, Scheme> schemes,
        IReadOnlyDictionary<string, Ritual> rituals)
    {
        Events = events;
        Schemes = schemes;
        Rituals = rituals;
    }

    public static ContentCatalog Load(string contentDir)
    {
        List<EventCard> events = LoadAll<EventCard>(Path.Combine(contentDir, "events"));
        List<Scheme> schemes = LoadAll<Scheme>(Path.Combine(contentDir, "schemes"));
        List<Ritual> rituals = LoadAll<Ritual>(Path.Combine(contentDir, "rituals"));

        return new ContentCatalog(
            events,
            schemes.Where(s => s.Id.Length > 0).ToDictionary(s => s.Id),
            rituals.Where(r => r.Id.Length > 0).ToDictionary(r => r.Id));
    }

    private static List<T> LoadAll<T>(string dir)
    {
        var result = new List<T>();
        if (!Directory.Exists(dir))
        {
            return result;
        }

        foreach (string file in Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            List<T>? items = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(file), SimJson.Content);
            if (items is not null)
            {
                result.AddRange(items);
            }
        }

        return result;
    }
}
