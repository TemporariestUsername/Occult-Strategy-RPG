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
    string contentDir = Path.Combine(root, "content");

    if (!File.Exists(registryPath))
    {
        Console.Error.WriteLine($"Missing content registry: {registryPath}");
        return 2;
    }

    SchemaVocabulary vocab = SchemaVocabulary.Load(schemaPath);
    ContentRegistry registry = ContentRegistry.Load(registryPath);

    List<string> files = Directory
        .EnumerateFiles(contentDir, "*.json", SearchOption.AllDirectories)
        .Where(f => !string.Equals(Path.GetFileName(f), "registry.json", StringComparison.Ordinal))
        .OrderBy(f => f, StringComparer.Ordinal)
        .ToList();

    var diagnostics = new List<Diagnostic>();
    var cards = new List<(string File, JsonElement Card)>();
    var allEventIds = new HashSet<string>(StringComparer.Ordinal);
    var seenIds = new HashSet<string>(StringComparer.Ordinal);
    var docs = new List<JsonDocument>();

    // First pass: parse every file and collect event ids (so cross-file
    // next_event references can be checked in the second pass).
    foreach (string file in files)
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
            diagnostics.Add(new Diagnostic(Severity.Error, rel, string.Empty, "content file must be a JSON array of event cards"));
            continue;
        }

        foreach (JsonElement card in doc.RootElement.EnumerateArray())
        {
            cards.Add((rel, card));
            if (card.ValueKind == JsonValueKind.Object
                && card.TryGetProperty("id", out JsonElement idEl)
                && idEl.ValueKind == JsonValueKind.String)
            {
                string id = idEl.GetString()!;
                if (!seenIds.Add(id))
                {
                    diagnostics.Add(new Diagnostic(Severity.Error, rel, id, $"duplicate event id '{id}'"));
                }

                allEventIds.Add(id);
            }
        }
    }

    // Second pass: validate every card.
    foreach ((string file, JsonElement card) in cards)
    {
        new EventCardValidator(vocab, registry, allEventIds, diagnostics, file).Validate(card);
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

    Console.WriteLine(
        $"Validated {cards.Count} card(s) across {files.Count} file(s): {errors} error(s), {warnings} warning(s).");
    return errors > 0 ? 1 : 0;
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
