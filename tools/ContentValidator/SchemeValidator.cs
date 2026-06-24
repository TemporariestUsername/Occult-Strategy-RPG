using System.Text.Json;

namespace PaleCommunion.Tools.ContentValidator;

/// <summary>Validates one scheme: its top-level shape plus the shared vocabulary.</summary>
public sealed class SchemeValidator : ContentItemValidator
{
    public SchemeValidator(
        SchemaVocabulary vocab,
        ContentRegistry registry,
        IReadOnlySet<string> allEventIds,
        List<Diagnostic> diagnostics,
        string file)
        : base(vocab, registry, allEventIds, diagnostics, file)
    {
    }

    public void Validate(JsonElement scheme)
    {
        if (scheme.ValueKind != JsonValueKind.Object)
        {
            SetItemId(string.Empty);
            Error("scheme is not a JSON object");
            return;
        }

        ValidateIdAndPattern(scheme);
        RequireString(scheme, "title");

        if (!scheme.TryGetProperty("duration", out JsonElement duration)
            || duration.ValueKind != JsonValueKind.Number
            || !duration.TryGetInt32(out int turns)
            || turns < 1)
        {
            Error("scheme needs an integer 'duration' >= 1");
        }

        if (!string.IsNullOrEmpty(ItemId) && Registry.KnowsCategory("schemes") && !Registry.Has("schemes", ItemId))
        {
            Warn("scheme id is not registered in content/registry.json 'schemes' (unlock_scheme / on_action references will not resolve)");
        }

        DeclareScopes(scheme);
        ValidateRequirements(scheme);
        ValidateCostArray(scheme);
        ValidateTokens(GetString(scheme, "description"));
        ValidateOutcomeBranches(scheme);
    }
}
