using System.Text.Json;
using System.Text.RegularExpressions;

namespace PaleCommunion.Tools.ContentValidator;

/// <summary>
/// Shared validation for any content item that uses the event vocabulary — events,
/// schemes, rituals. Validates conditions, effects, checks, costs, selectors, outcomes,
/// and scope/registry integrity. Subclasses add the top-level shape for their type.
/// Errors are wrong regardless of how much content exists; warnings are forward
/// references that depend on not-yet-authored content.
/// </summary>
public abstract class ContentItemValidator
{
    private static readonly Regex ScopeToken =
        new(@"\{([a-z_][a-z0-9_]*)\.[a-z_][a-z0-9_]*\}", RegexOptions.Compiled);

    protected SchemaVocabulary Vocab { get; }
    protected ContentRegistry Registry { get; }
    protected IReadOnlySet<string> AllEventIds { get; }

    private readonly List<Diagnostic> _diagnostics;
    private readonly string _file;

    protected string ItemId { get; private set; } = string.Empty;
    private HashSet<string> _scopes = new(StringComparer.Ordinal);

    protected ContentItemValidator(
        SchemaVocabulary vocab,
        ContentRegistry registry,
        IReadOnlySet<string> allEventIds,
        List<Diagnostic> diagnostics,
        string file)
    {
        Vocab = vocab;
        Registry = registry;
        AllEventIds = allEventIds;
        _diagnostics = diagnostics;
        _file = file;
    }

    protected void SetItemId(string id) => ItemId = id;

    protected void ValidateIdAndPattern(JsonElement item)
    {
        ItemId = GetString(item, "id") ?? string.Empty;
        RequireString(item, "id");
        if (!string.IsNullOrEmpty(ItemId) && !Vocab.IdPattern.IsMatch(ItemId))
        {
            Error($"id does not match required pattern {Vocab.IdPattern}");
        }
    }

    protected void DeclareScopes(JsonElement item)
    {
        _scopes = new HashSet<string>(StringComparer.Ordinal);
        if (item.TryGetProperty("bindings", out JsonElement bindings) && bindings.ValueKind == JsonValueKind.Object)
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
    }

    /// <summary>Validate the check/outcome/on_* resolution block shared by schemes and rituals.</summary>
    protected void ValidateOutcomeBranches(JsonElement item)
    {
        bool hasOutcome = item.TryGetProperty("outcome", out JsonElement outcome);
        bool hasCheck = item.TryGetProperty("check", out JsonElement check);
        bool hasSuccess = item.TryGetProperty("on_success", out _);
        if (!hasOutcome && !hasCheck && !hasSuccess)
        {
            Error("needs an 'outcome', a 'check', or 'on_success'");
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
            if (item.TryGetProperty(branch, out JsonElement outcomeBranch))
            {
                ValidateOutcome(outcomeBranch);
            }
        }
    }

    protected void ValidateCostArray(JsonElement item)
    {
        if (item.TryGetProperty("cost", out JsonElement cost) && cost.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement entry in cost.EnumerateArray())
            {
                ValidateCost(entry);
            }
        }
    }

    protected void ValidateRequirements(JsonElement item)
    {
        if (item.TryGetProperty("requirements", out JsonElement requirements))
        {
            ValidateCondition(requirements);
        }
    }

    protected void ValidateSelector(JsonElement selector)
    {
        if (selector.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (selector.TryGetProperty("from", out JsonElement from) && from.ValueKind == JsonValueKind.String
            && !Vocab.SelectorFrom.Contains(from.GetString()!))
        {
            Error($"selector 'from' has unknown value '{from.GetString()}'");
        }

        if (selector.TryGetProperty("prefer", out JsonElement prefer) && prefer.ValueKind == JsonValueKind.String
            && !Vocab.SelectorPrefer.Contains(prefer.GetString()!))
        {
            Error($"selector 'prefer' has unknown value '{prefer.GetString()}'");
        }

        if (selector.TryGetProperty("require", out JsonElement require) && require.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement leaf in require.EnumerateArray())
            {
                ValidateCondition(leaf);
            }
        }
    }

    protected void ValidateCost(JsonElement cost)
    {
        string? resource = GetString(cost, "resource");
        if (string.IsNullOrEmpty(resource))
        {
            Error("cost entry missing 'resource'");
        }
        else if (!Registry.Has("resources", resource))
        {
            Error($"cost references unknown resource '{resource}'");
        }

        if (!cost.TryGetProperty("amount", out JsonElement amount) || amount.ValueKind != JsonValueKind.Number)
        {
            Error("cost entry missing numeric 'amount'");
        }
    }

    protected void ValidateCheck(JsonElement check)
    {
        bool hasAttr = check.TryGetProperty("attribute", out JsonElement attr) && attr.ValueKind == JsonValueKind.String;
        bool hasSkill = check.TryGetProperty("skill", out JsonElement skill) && skill.ValueKind == JsonValueKind.String;

        if (hasAttr && !Vocab.Attributes.Contains(attr.GetString()!))
        {
            Error($"unknown attribute '{attr.GetString()}'");
        }

        if (hasSkill && !Vocab.Skills.Contains(skill.GetString()!))
        {
            Error($"unknown skill '{skill.GetString()}'");
        }

        if (!hasAttr && !hasSkill)
        {
            Error("check must specify at least one of 'attribute' or 'skill'");
        }

        if (!check.TryGetProperty("difficulty", out JsonElement difficulty) || difficulty.ValueKind != JsonValueKind.Number)
        {
            Error("check missing numeric 'difficulty'");
        }

        RequireScope(GetString(check, "scope") ?? "actor");

        if (check.TryGetProperty("modifiers", out JsonElement modifiers) && modifiers.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement modifier in modifiers.EnumerateArray())
            {
                if (!modifier.TryGetProperty("value", out JsonElement value) || value.ValueKind != JsonValueKind.Number)
                {
                    Error("check modifier missing numeric 'value'");
                }

                if (modifier.TryGetProperty("when", out JsonElement when))
                {
                    ValidateCondition(when);
                }
            }
        }
    }

    protected void ValidateOutcome(JsonElement outcome)
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
        if (!string.IsNullOrEmpty(next) && !AllEventIds.Contains(next))
        {
            Warn($"next_event '{next}' refers to an event id that does not exist yet");
        }
    }

    protected void ValidateEffect(JsonElement effect)
    {
        if (effect.ValueKind != JsonValueKind.Object)
        {
            Error("effect is not an object");
            return;
        }

        string? type = GetString(effect, "type");
        if (string.IsNullOrEmpty(type))
        {
            Error("effect missing 'type'");
            return;
        }

        if (!Vocab.EffectTypes.Contains(type))
        {
            Error($"unknown effect type '{type}'");
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
                if (!string.IsNullOrEmpty(status) && !Vocab.Statuses.Contains(status))
                {
                    Error($"unknown member status '{status}'");
                }

                break;
            case "relationship":
                string? kind = GetString(effect, "kind");
                if (!string.IsNullOrEmpty(kind) && !Vocab.Kinds.Contains(kind))
                {
                    Error($"unknown relationship kind '{kind}'");
                }

                break;
            case "queue_event":
                if (!string.IsNullOrEmpty(id) && !AllEventIds.Contains(id))
                {
                    Warn($"queue_event '{id}' refers to an event id that does not exist yet");
                }

                break;
        }
    }

    protected void ValidateCondition(JsonElement condition)
    {
        if (condition.ValueKind != JsonValueKind.Object)
        {
            Error("condition is not an object");
            return;
        }

        string? type = GetString(condition, "type");
        if (string.IsNullOrEmpty(type))
        {
            Error("condition missing 'type'");
            return;
        }

        if (!Vocab.ConditionTypes.Contains(type))
        {
            Error($"unknown condition type '{type}'");
            return;
        }

        if (Vocab.ConditionGroupTypes.Contains(type))
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
                Error($"condition group '{type}' missing 'conditions' array");
            }

            return;
        }

        if (condition.TryGetProperty("op", out JsonElement op) && op.ValueKind == JsonValueKind.String
            && !Vocab.Ops.Contains(op.GetString()!))
        {
            Error($"unknown comparison op '{op.GetString()}'");
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
                if (!string.IsNullOrEmpty(key) && !Vocab.Skills.Contains(key))
                {
                    Error($"unknown skill '{key}'");
                }

                break;
            case "scope_attribute":
                if (!string.IsNullOrEmpty(key) && !Vocab.Attributes.Contains(key))
                {
                    Error($"unknown attribute '{key}'");
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

    protected void CheckRef(string category, string? value, string field)
    {
        if (string.IsNullOrEmpty(value))
        {
            Error($"missing required '{field}'");
            return;
        }

        if (Registry.KnowsCategory(category) && !Registry.Has(category, value))
        {
            Error($"unknown {field} '{value}' (not registered in content/registry.json '{category}')");
        }
    }

    protected void RequireScope(string scope)
    {
        if (!string.IsNullOrEmpty(scope) && !_scopes.Contains(scope))
        {
            Error($"references scope '{scope}' which is not declared in bindings");
        }
    }

    protected void ValidateTokens(string? text)
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

    protected void RequireString(JsonElement obj, string name)
    {
        if (GetString(obj, name) is null)
        {
            Error($"missing required string '{name}'");
        }
    }

    protected static string? GetString(JsonElement obj, string name) =>
        obj.ValueKind == JsonValueKind.Object
        && obj.TryGetProperty(name, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    protected void Error(string message) =>
        _diagnostics.Add(new Diagnostic(Severity.Error, _file, ItemId, message));

    protected void Warn(string message) =>
        _diagnostics.Add(new Diagnostic(Severity.Warning, _file, ItemId, message));
}
