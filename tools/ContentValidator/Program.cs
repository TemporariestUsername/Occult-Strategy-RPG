using System.Text.Json;
using PaleCommunion.Tools.ContentValidator;

return Run();

static int Run()
{
    string? root = FindRepoRoot(Directory.GetCurrentDirectory());
    if (root is null)
    {
        Console.Error.WriteLine("Could not locate repo root (no schemas/event.schema.json in any parent directory).");
        return 2;
    }

    string schemaPath = Path.Combine(root, "schemas", "event.schema.json");
    string registryPath = Path.Combine(root, "content", "registry.json");
    if (!File.Exists(registryPath))
    {
        Console.Error.WriteLine($"Missing content registry: {registryPath}");
        return 2;
    }

    // Schemes and rituals reuse the event vocabulary, so one SchemaVocabulary serves all.
    SchemaVocabulary vocab = SchemaVocabulary.Load(schemaPath);
    ContentRegistry registry = ContentRegistry.Load(registryPath);

    var diagnostics = new List<Diagnostic>();
    var docs = new List<JsonDocument>();

    List<ContentItem> events = LoadType(root, "events", diagnostics, docs);
    List<ContentItem> schemes = LoadType(root, "schemes", diagnostics, docs);
    List<ContentItem> rituals = LoadType(root, "rituals", diagnostics, docs);

    var allEventIds = new HashSet<string>(
        events.Select(i => i.Id).Where(id => id.Length > 0), StringComparer.Ordinal);

    foreach (ContentItem item in events)
    {
        new EventCardValidator(vocab, registry, allEventIds, diagnostics, item.File).Validate(item.Element);
    }

    foreach (ContentItem item in schemes)
    {
        new SchemeValidator(vocab, registry, allEventIds, diagnostics, item.File).Validate(item.Element);
    }

    foreach (ContentItem item in rituals)
    {
        new RitualValidator(vocab, registry, allEventIds, diagnostics, item.File).Validate(item.Element);
    }

    foreach (JsonDocument doc in docs)
    {
        doc.Dispose();
    }

    int errors = diagnostics.Count(d => d.Severity == Severity.Error);
    int warnings = diagnostics.Count(d => d.Severity == Severity.Warning);

    foreach (Diagnostic d in diagnostics
        .OrderByDescending(d => d.Severity == Severity.Error)
        .ThenBy(d => d.File, StringComparer.Ordinal)
        .ThenBy(d => d.CardId, StringComparer.Ordinal))
    {
        Console.WriteLine(d);
    }

    if (diagnostics.Count > 0)
    {
        Console.WriteLine();
    }

    int total = events.Count + schemes.Count + rituals.Count;
    Console.WriteLine(
        $"Validated {total} item(s) ({events.Count} event(s), {schemes.Count} scheme(s), {rituals.Count} ritual(s)): " +
        $"{errors} error(s), {warnings} warning(s).");
    return errors > 0 ? 1 : 0;
}

// Parse every JSON array under content/<typeDir>/, collecting each item with its id and
// flagging duplicate ids within the type.
static List<ContentItem> LoadType(string root, string typeDir, List<Diagnostic> diagnostics, List<JsonDocument> docs)
{
    var items = new List<ContentItem>();
    string dir = Path.Combine(root, "content", typeDir);
    if (!Directory.Exists(dir))
    {
        return items;
    }

    var seen = new HashSet<string>(StringComparer.Ordinal);
    foreach (string file in Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories)
        .OrderBy(f => f, StringComparer.Ordinal))
    {
        string rel = Path.GetRelativePath(root, file);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(File.ReadAllText(file));
        }
        catch (JsonException ex)
        {
            diagnostics.Add(new Diagnostic(Severity.Error, rel, string.Empty, $"invalid JSON: {ex.Message}"));
            continue;
        }

        docs.Add(doc);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(new Diagnostic(Severity.Error, rel, string.Empty, $"{typeDir} file must be a JSON array"));
            continue;
        }

        foreach (JsonElement element in doc.RootElement.EnumerateArray())
        {
            string id = element.ValueKind == JsonValueKind.Object
                        && element.TryGetProperty("id", out JsonElement idEl)
                        && idEl.ValueKind == JsonValueKind.String
                ? idEl.GetString()!
                : string.Empty;

            if (id.Length > 0 && !seen.Add(id))
            {
                diagnostics.Add(new Diagnostic(Severity.Error, rel, id, $"duplicate {typeDir} id '{id}'"));
            }

            items.Add(new ContentItem(rel, element, id));
        }
    }

    return items;
}

static string? FindRepoRoot(string start)
{
    DirectoryInfo? dir = new(start);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "schemas", "event.schema.json")))
        {
            return dir.FullName;
        }

        dir = dir.Parent;
    }

    return null;
}

internal sealed record ContentItem(string File, JsonElement Element, string Id);
