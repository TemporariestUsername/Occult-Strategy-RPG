using System.Text.Json;

namespace PaleCommunion.Tools.ContentValidator;

/// <summary>Validates one event card: its top-level shape plus the shared vocabulary.</summary>
public sealed class EventCardValidator : ContentItemValidator
{
    public EventCardValidator(
        SchemaVocabulary vocab,
        ContentRegistry registry,
        IReadOnlySet<string> allEventIds,
        List<Diagnostic> diagnostics,
        string file)
        : base(vocab, registry, allEventIds, diagnostics, file)
    {
    }

    public void Validate(JsonElement card)
    {
        if (card.ValueKind != JsonValueKind.Object)
        {
            SetItemId(string.Empty);
            Error("card is not a JSON object");
            return;
        }

        ValidateIdAndPattern(card);
        RequireString(card, "title");
        RequireString(card, "body");

        bool hasChoices = card.TryGetProperty("choices", out JsonElement choices)
                          && choices.ValueKind == JsonValueKind.Array;
        if (!hasChoices)
        {
            Error("missing required 'choices' array");
        }
        else if (choices.GetArrayLength() == 0)
        {
            Error("'choices' must have at least one entry");
        }

        if (card.TryGetProperty("category", out JsonElement cat) && cat.ValueKind == JsonValueKind.String
            && !Vocab.Categories.Contains(cat.GetString()!))
        {
            Error($"unknown category '{cat.GetString()}'");
        }

        if (card.TryGetProperty("weight", out JsonElement weight)
            && weight.ValueKind == JsonValueKind.Number && weight.GetDouble() < 0)
        {
            Error("weight must be >= 0");
        }

        DeclareScopes(card);
        ValidateTokens(GetString(card, "title"));
        ValidateTokens(GetString(card, "body"));

        if (card.TryGetProperty("on_appear", out JsonElement onAppear) && onAppear.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement effect in onAppear.EnumerateArray())
            {
                ValidateEffect(effect);
            }
        }

        if (card.TryGetProperty("trigger", out JsonElement trigger) && trigger.ValueKind == JsonValueKind.Object)
        {
            ValidateTrigger(trigger);
        }

        if (hasChoices)
        {
            foreach (JsonElement choice in choices.EnumerateArray())
            {
                ValidateChoice(choice);
            }
        }
    }

    private void ValidateTrigger(JsonElement trigger)
    {
        string? type = GetString(trigger, "type");
        if (type is not null && !Vocab.TriggerTypes.Contains(type))
        {
            Error($"trigger 'type' has unknown value '{type}'");
        }

        if (trigger.TryGetProperty("conditions", out JsonElement conditions))
        {
            ValidateCondition(conditions);
        }

        if (type == "on_action")
        {
            string? action = GetString(trigger, "action");
            if (string.IsNullOrEmpty(action))
            {
                Warn("trigger type 'on_action' has no 'action'");
            }
            else if (!Registry.Has("schemes", action) && !Registry.Has("rituals", action))
            {
                Warn($"trigger action '{action}' is not a known scheme or ritual");
            }
        }
    }

    private void ValidateChoice(JsonElement choice)
    {
        if (choice.ValueKind != JsonValueKind.Object)
        {
            Error("choice is not an object");
            return;
        }

        RequireString(choice, "id");
        RequireString(choice, "label");
        ValidateTokens(GetString(choice, "label"));
        ValidateTokens(GetString(choice, "tooltip"));
        ValidateTokens(GetString(choice, "locked_hint"));

        bool hasOutcome = choice.TryGetProperty("outcome", out JsonElement outcome);
        bool hasCheck = choice.TryGetProperty("check", out JsonElement check);
        if (!hasOutcome && !hasCheck)
        {
            Error($"choice '{GetString(choice, "id")}' must have either 'outcome' or 'check'");
        }

        ValidateRequirements(choice);

        if (choice.TryGetProperty("requirement_mode", out JsonElement mode) && mode.ValueKind == JsonValueKind.String
            && !Vocab.RequirementModes.Contains(mode.GetString()!))
        {
            Error($"unknown requirement_mode '{mode.GetString()}'");
        }

        ValidateCostArray(choice);

        if (hasCheck)
        {
            ValidateCheck(check);
        }

        if (hasOutcome)
        {
            ValidateOutcome(outcome);
        }

        foreach (string branch in new[] { "on_success", "on_failure", "on_critical_success", "on_critical_failure" })
        {
            if (choice.TryGetProperty(branch, out JsonElement outcomeBranch))
            {
                ValidateOutcome(outcomeBranch);
            }
        }
    }
}
