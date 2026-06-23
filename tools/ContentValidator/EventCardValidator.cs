using System.Text.Json;
using System.Text.RegularExpressions;

namespace PaleCommunion.Tools.ContentValidator;

/// <summary>
/// Validates one event card against the schema vocabulary and the content registry,
/// appending diagnostics. Errors are things that are wrong regardless of how much
/// content exists yet (bad id, unknown effect type, undeclared scope, unregistered
/// content id). Warnings are cross-references that depend on not-yet-authored
/// content (a next_event/queue_event target that does not exist).
/// </summary>
public sealed class EventCardValidator
{
    private static readonly Regex ScopeToken =
        new(@"\{([a-z_][a-z0-9_]*)\.[a-z_][a-z0-9_]*\}", RegexOptions.Compiled);

    private readonly SchemaVocabulary _vocab;
    private readonly ContentRegistry _registry;
    private readonly IReadOnlySet<string> _allEventIds;
    private readonly List<Diagnostic> _diagnostics;
    private readonly string _file;

    private string _cardId = string.Empty;
    private HashSet<string> _scopes = new(StringComparer.Ordinal);

    public EventCardValidator(
        SchemaVocabulary vocab,
        ContentRegistry registry,
        IReadOnlySet<string> allEventIds,
        List<Diagnostic> diagnostics,
        string file)
    {
        _vocab = vocab;
        _registry = registry;
        _allEventIds = allEventIds;
        _diagnostics = diagnostics;
        _file = file;
    }

    public void Validate(JsonElement card)
    {
        if (card.ValueKind != JsonValueKind.Object)
        {
            Error(string.Empty, "card is not a JSON object");
            return;
        }

        _cardId = GetString(card, "id") ?? string.Empty;
        RequireString(card, "id");
        RequireString(card, "title");
        RequireString(card, "body");

        bool hasChoices = card.TryGetProperty("choices", out JsonElement choices)
                          && choices.ValueKind == JsonValueKind.Array;
        if (!hasChoices)
        {
            Error(_cardId, "missing required 'choices' array");
        }
        else if (choices.GetArrayLength() == 0)
        {
            Error(_cardId, "'choices' must have at least one entry");
        }

        if (!string.IsNullOrEmpty(_cardId) && !_vocab.IdPattern.IsMatch(_cardId))
        {
            Error(_cardId, $"id does not match required pattern {_vocab.IdPattern}");
        }

        if (card.TryGetProperty("category", out JsonElement cat) && cat.ValueKind == JsonValueKind.String
            && !_vocab.Categories.Contains(cat.GetString()!))
        {
            Error(_cardId, $"unknown category '{cat.GetString()}'");
        }

        if (card.TryGetProperty("weight", out JsonElement weight)
            && weight.ValueKind == JsonValueKind.Number && weight.GetDouble() < 0)
        {
            Error(_cardId, "weight must be >= 0");
        }

        _scopes = new HashSet<string>(StringComparer.Ordinal);
        if (card.TryGetProperty("bindings", out JsonElement bindings) && bindings.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty binding in bindings.EnumerateObject())
            {
                _scopes.Add(binding.Name);
            }

            foreach (JsonProperty binding in bindings.EnumerateObject())
            {
                ValidateSelector(binding.Value);
            }
        }

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

    private void ValidateSelector(JsonElement selector)
    {
        if (selector.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (selector.TryGetProperty("from", out JsonElement from) && from.ValueKind == JsonValueKind.String
            && !_vocab.SelectorFrom.Contains(from.GetString()!))
        {
            Error(_cardId, $"selector 'from' has unknown value '{from.GetString()}'");
        }

        if (selector.TryGetProperty("prefer", out JsonElement prefer) && prefer.ValueKind == JsonValueKind.String
            && !_vocab.SelectorPrefer.Contains(prefer.GetString()!))
        {
            Error(_cardId, $"selector 'prefer' has unknown value '{prefer.GetString()}'");
        }

        if (selector.TryGetProperty("require", out JsonElement require) && require.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement leaf in require.EnumerateArray())
            {
                ValidateCondition(leaf);
            }
        }
    }

    private void ValidateTrigger(JsonElement trigger)
    {
        string? type = GetString(trigger, "type");
        if (type is not null && !_vocab.TriggerTypes.Contains(type))
        {
            Error(_cardId, $"trigger 'type' has unknown value '{type}'");
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
                Warn(_cardId, "trigger type 'on_action' has no 'action'");
            }
            else if (!_registry.Has("schemes", action) && !_registry.Has("rituals", action))
            {
                Warn(_cardId, $"trigger action '{action}' is not a known scheme or ritual");
            }
        }
    }

    private void ValidateChoice(JsonElement choice)
    {
        if (choice.ValueKind != JsonValueKind.Object)
        {
            Error(_cardId, "choice is not an object");
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
            Error(_cardId, $"choice '{GetString(choice, "id")}' must have either 'outcome' or 'check'");
        }

        if (choice.TryGetProperty("requirements", out JsonElement requirements))
        {
            ValidateCondition(requirements);
        }

        if (choice.TryGetProperty("requirement_mode", out JsonElement mode) && mode.ValueKind == JsonValueKind.String
            && !_vocab.RequirementModes.Contains(mode.GetString()!))
        {
            Error(_cardId, $"unknown requirement_mode '{mode.GetString()}'");
        }

        if (choice.TryGetProperty("cost", out JsonElement cost) && cost.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement entry in cost.EnumerateArray())
            {
                ValidateCost(entry);
            }
        }

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

    private void ValidateCost(JsonElement cost)
    {
        string? resource = GetString(cost, "resource");
        if (string.IsNullOrEmpty(resource))
        {
            Error(_cardId, "cost entry missing 'resource'");
        }
        else if (!_registry.Has("resources", resource))
        {
            Error(_cardId, $"cost references unknown resource '{resource}'");
        }

        if (!cost.TryGetProperty("amount", out JsonElement amount) || amount.ValueKind != JsonValueKind.Number)
        {
            Error(_cardId, "cost entry missing numeric 'amount'");
        }
    }

    private void ValidateCheck(JsonElement check)
    {
        bool hasAttr = check.TryGetProperty("attribute", out JsonElement attr) && attr.ValueKind == JsonValueKind.String;
        bool hasSkill = check.TryGetProperty("skill", out JsonElement skill) && skill.ValueKind == JsonValueKind.String;

        if (hasAttr && !_vocab.Attributes.Contains(attr.GetString()!))
        {
            Error(_cardId, $"unknown attribute '{attr.GetString()}'");
        }

        if (hasSkill && !_vocab.Skills.Contains(skill.GetString()!))
        {
            Error(_cardId, $"unknown skill '{skill.GetString()}'");
        }

        if (!hasAttr && !hasSkill)
        {
            Error(_cardId, "check must specify at least one of 'attribute' or 'skill'");
        }

        if (!check.TryGetProperty("difficulty", out JsonElement difficulty) || difficulty.ValueKind != JsonValueKind.Number)
        {
            Error(_cardId, "check missing numeric 'difficulty'");
        }

        RequireScope(GetString(check, "scope") ?? "actor");

        if (check.TryGetProperty("modifiers", out JsonElement modifiers) && modifiers.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement modifier in modifiers.EnumerateArray())
            {
                if (!modifier.TryGetProperty("value", out JsonElement value) || value.ValueKind != JsonValueKind.Number)
                {
                    Error(_cardId, "check modifier missing numeric 'value'");
                }

                if (modifier.TryGetProperty("when", out JsonElement when))
                {
                    ValidateCondition(when);
                }
            }
        }
    }

    private void ValidateOutcome(JsonElement outcome)
    {
        if (outcome.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        ValidateTokens(GetString(outcome, "result_text"));

        if (outcome.TryGetProperty("effects", out JsonElement effects) && effects.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement effect in effects.EnumerateArray())
            {
                ValidateEffect(effect);
            }
        }

        string? next = GetString(outcome, "next_event");
        if (!string.IsNullOrEmpty(next) && !_allEventIds.Contains(next))
        {
            Warn(_cardId, $"next_event '{next}' refers to an event id that does not exist yet");
        }
    }

    private void ValidateEffect(JsonElement effect)
    {
        if (effect.ValueKind != JsonValueKind.Object)
        {
            Error(_cardId, "effect is not an object");
            return;
        }

        string? type = GetString(effect, "type");
        if (string.IsNullOrEmpty(type))
        {
            Error(_cardId, "effect missing 'type'");
            return;
        }

        if (!_vocab.EffectTypes.Contains(type))
        {
            Error(_cardId, $"unknown effect type '{type}'");
            return;
        }

        foreach (string scopeField in new[] { "scope", "target_scope", "about_scope" })
        {
            string? scope = GetString(effect, scopeField);
            if (scope is not null)
            {
                RequireScope(scope);
            }
        }

        string? key = GetString(effect, "key");
        string? id = GetString(effect, "id");
        switch (type)
        {
            case "resource": CheckRef("resources", key, "key"); break;
            case "attention": CheckRef("attention_channels", key, "key"); break;
            case "institution_influence":
            case "institution_disposition": CheckRef("institutions", key, "key"); break;
            case "add_trait":
            case "remove_trait": CheckRef("traits", id, "id"); break;
            case "grant_relic":
            case "remove_relic": CheckRef("relics", id, "id"); break;
            case "grant_reagent":
            case "remove_reagent": CheckRef("reagent_items", id, "id"); break;
            case "unlock_ritual": CheckRef("rituals", id, "id"); break;
            case "unlock_scheme": CheckRef("schemes", id, "id"); break;
            case "reveal_secret":
            case "gain_secret": CheckRef("secrets", id, "id"); break;
            case "patron_relationship": CheckRef("patrons", id, "id"); break;
            case "great_work_advance":
            case "great_work_set_step":
                if (!string.IsNullOrEmpty(id)) { CheckRef("great_works", id, "id"); }
                break;
            case "recruit":
                string? template = GetString(effect, "template");
                if (!string.IsNullOrEmpty(template)) { CheckRef("recruit_templates", template, "template"); }
                break;
            case "member_status":
                string? status = GetString(effect, "status");
                if (!string.IsNullOrEmpty(status) && !_vocab.Statuses.Contains(status))
                {
                    Error(_cardId, $"unknown member status '{status}'");
                }

                break;
            case "relationship":
                string? kind = GetString(effect, "kind");
                if (!string.IsNullOrEmpty(kind) && !_vocab.Kinds.Contains(kind))
                {
                    Error(_cardId, $"unknown relationship kind '{kind}'");
                }

                break;
            case "queue_event":
                if (!string.IsNullOrEmpty(id) && !_allEventIds.Contains(id))
                {
                    Warn(_cardId, $"queue_event '{id}' refers to an event id that does not exist yet");
                }

                break;
        }
    }

    private void ValidateCondition(JsonElement condition)
    {
        if (condition.ValueKind != JsonValueKind.Object)
        {
            Error(_cardId, "condition is not an object");
            return;
        }

        string? type = GetString(condition, "type");
        if (string.IsNullOrEmpty(type))
        {
            Error(_cardId, "condition missing 'type'");
            return;
        }

        if (!_vocab.ConditionTypes.Contains(type))
        {
            Error(_cardId, $"unknown condition type '{type}'");
            return;
        }

        if (_vocab.ConditionGroupTypes.Contains(type))
        {
            if (condition.TryGetProperty("conditions", out JsonElement subs) && subs.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement sub in subs.EnumerateArray())
                {
                    ValidateCondition(sub);
                }
            }
            else
            {
                Error(_cardId, $"condition group '{type}' missing 'conditions' array");
            }

            return;
        }

        if (condition.TryGetProperty("op", out JsonElement op) && op.ValueKind == JsonValueKind.String
            && !_vocab.Ops.Contains(op.GetString()!))
        {
            Error(_cardId, $"unknown comparison op '{op.GetString()}'");
        }

        string? scope = GetString(condition, "scope");
        if (scope is not null)
        {
            RequireScope(scope);
        }

        string? key = GetString(condition, "key");
        string? id = GetString(condition, "id");
        switch (type)
        {
            case "resource": CheckRef("resources", key, "key"); break;
            case "attention": CheckRef("attention_channels", key, "key"); break;
            case "institution_influence":
            case "institution_disposition": CheckRef("institutions", key, "key"); break;
            case "member_with_skill":
            case "scope_skill":
                if (!string.IsNullOrEmpty(key) && !_vocab.Skills.Contains(key))
                {
                    Error(_cardId, $"unknown skill '{key}'");
                }

                break;
            case "scope_attribute":
                if (!string.IsNullOrEmpty(key) && !_vocab.Attributes.Contains(key))
                {
                    Error(_cardId, $"unknown attribute '{key}'");
                }

                break;
            case "member_with_trait":
            case "scope_has_trait": CheckRef("traits", id, "id"); break;
            case "has_relic": CheckRef("relics", id, "id"); break;
            case "has_reagent": CheckRef("reagent_items", id, "id"); break;
            case "patron_relationship": CheckRef("patrons", id, "id"); break;
            case "secret_known": CheckRef("secrets", id, "id"); break;
            case "great_work_chosen":
            case "great_work_step":
                if (!string.IsNullOrEmpty(id)) { CheckRef("great_works", id, "id"); }
                break;
        }
    }

    private void CheckRef(string category, string? value, string field)
    {
        if (string.IsNullOrEmpty(value))
        {
            Error(_cardId, $"missing required '{field}'");
            return;
        }

        if (_registry.KnowsCategory(category) && !_registry.Has(category, value))
        {
            Error(_cardId, $"unknown {field} '{value}' (not registered in content/registry.json '{category}')");
        }
    }

    private void RequireScope(string scope)
    {
        if (!string.IsNullOrEmpty(scope) && !_scopes.Contains(scope))
        {
            Error(_cardId, $"references scope '{scope}' which is not declared in bindings");
        }
    }

    private void ValidateTokens(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach (Match match in ScopeToken.Matches(text))
        {
            RequireScope(match.Groups[1].Value);
        }
    }

    private void RequireString(JsonElement obj, string name)
    {
        if (GetString(obj, name) is null)
        {
            Error(_cardId, $"missing required string '{name}'");
        }
    }

    private static string? GetString(JsonElement obj, string name) =>
        obj.ValueKind == JsonValueKind.Object
        && obj.TryGetProperty(name, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private void Error(string cardId, string message) =>
        _diagnostics.Add(new Diagnostic(Severity.Error, _file, cardId, message));

    private void Warn(string cardId, string message) =>
        _diagnostics.Add(new Diagnostic(Severity.Warning, _file, cardId, message));
}
