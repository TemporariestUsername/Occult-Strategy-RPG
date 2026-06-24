using System.Text.Json;

namespace PaleCommunion.Tools.ContentValidator;

/// <summary>Validates one ritual: its top-level shape plus the shared vocabulary.</summary>
public sealed class RitualValidator : ContentItemValidator
{
    public RitualValidator(
        SchemaVocabulary vocab,
        ContentRegistry registry,
        IReadOnlySet<string> allEventIds,
        List<Diagnostic> diagnostics,
        string file)
        : base(vocab, registry, allEventIds, diagnostics, file)
    {
    }

    public void Validate(JsonElement ritual)
    {
        if (ritual.ValueKind != JsonValueKind.Object)
        {
            SetItemId(string.Empty);
            Error("ritual is not a JSON object");
            return;
        }

        ValidateIdAndPattern(ritual);
        RequireString(ritual, "title");

        if (!string.IsNullOrEmpty(ItemId) && Registry.KnowsCategory("rituals") && !Registry.Has("rituals", ItemId))
        {
            Warn("ritual id is not registered in content/registry.json 'rituals' (unlock_ritual references will not resolve)");
        }

        DeclareScopes(ritual);
        ValidateRequirements(ritual);
        ValidateCostArray(ritual);
        ValidateReagents(ritual);
        ValidateTokens(GetString(ritual, "description"));

        if (ritual.TryGetProperty("on_perform", out JsonElement onPerform) && onPerform.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement effect in onPerform.EnumerateArray())
            {
                ValidateEffect(effect);
            }
        }

        ValidateOutcomeBranches(ritual);
    }

    private void ValidateReagents(JsonElement ritual)
    {
        if (!ritual.TryGetProperty("reagents", out JsonElement reagents) || reagents.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement reagent in reagents.EnumerateArray())
        {
            CheckRef("reagent_items", GetString(reagent, "id"), "id");
            if (reagent.TryGetProperty("count", out JsonElement count)
                && (count.ValueKind != JsonValueKind.Number || !count.TryGetInt32(out int c) || c < 1))
            {
                Error("reagent 'count' must be an integer >= 1");
            }
        }
    }
}
