using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>Outcome of applying a batch of effects: what changed, and what isn't wired up yet.</summary>
public sealed class EffectResult
{
    public List<string> Applied { get; } = new();
    public List<string> Unhandled { get; } = new();

    public bool FullyApplied => Unhandled.Count == 0;
}

/// <summary>
/// Applies the typed effect vocabulary to a <see cref="GameState"/>. Character-scoped
/// effects resolve their target through the <see cref="BindingContext"/>. Great Work
/// effects are valid vocabulary whose handlers ship with that system; until then they
/// are recorded as Unhandled rather than applied, so the boundary stays explicit (law 5).
/// </summary>
public static class EffectEngine
{
    private static readonly HashSet<string> HandledTypeSet = new(StringComparer.Ordinal)
    {
        "resource", "veil", "devotion", "order_corruption", "attention",
        "institution_influence", "institution_disposition", "set_flag",
        "add_trait", "remove_trait", "member_corruption", "member_status", "kill_member",
        "recruit", "relationship", "grant_relic", "remove_relic", "grant_reagent", "remove_reagent",
        "gain_secret", "reveal_secret", "patron_relationship", "unlock_ritual", "unlock_scheme",
        "tier_change", "queue_event", "end_game",
    };

    /// <summary>The effect types this build actually simulates.</summary>
    public static IReadOnlyCollection<string> HandledTypes => HandledTypeSet;

    public static EffectResult Apply(
        GameState state, IEnumerable<Effect> effects, IRng rng, BindingContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(rng);

        var result = new EffectResult();
        foreach (Effect effect in effects)
        {
            if (ApplyOne(state, effect, context))
            {
                result.Applied.Add(effect.Type);
            }
            else
            {
                result.Unhandled.Add(effect.Type);
            }
        }

        state.Normalize();
        return result;
    }

    private static bool ApplyOne(GameState s, Effect e, BindingContext? ctx)
    {
        switch (e.Type)
        {
            // Order-level economy.
            case "resource": AddResource(s, e.Key, e.Amount ?? 0); return true;
            case "veil": s.Veil += e.Amount ?? 0; return true;
            case "devotion": s.Devotion += e.Amount ?? 0; return true;
            case "order_corruption": s.OrderCorruption += e.Amount ?? 0; return true;
            case "attention": AddAttention(s, e.Key, e.Amount ?? 0); return true;
            case "institution_influence": InstitutionFor(s, e.Key).Influence += e.Amount ?? 0; return true;
            case "institution_disposition": InstitutionFor(s, e.Key).Disposition += e.Amount ?? 0; return true;
            case "set_flag": s.Flags[Require(e.Id, "set_flag.id")] = ConditionEvaluator.AsBool(e.Value) ?? true; return true;

            // Characters.
            case "add_trait": Bound(ctx, e.Scope).Traits.Add(Require(e.Id, "add_trait.id")); return true;
            case "remove_trait": Bound(ctx, e.Scope).Traits.Remove(Require(e.Id, "remove_trait.id")); return true;
            case "member_corruption": Bound(ctx, e.Scope).Corruption += e.Amount ?? 0; return true;
            case "member_status": Bound(ctx, e.Scope).Status = ParseStatus(e.Status); return true;
            case "kill_member": Bound(ctx, e.Scope).Status = MemberStatus.Dead; return true;
            case "recruit": Recruit(s, e.Template, e.Count ?? 1); return true;
            case "relationship": AddRelationship(s, ctx, e); return true;

            // Inventory and the occult ledger.
            case "grant_relic": s.Relics.Add(Require(e.Id, "grant_relic.id")); return true;
            case "remove_relic": s.Relics.Remove(Require(e.Id, "remove_relic.id")); return true;
            case "grant_reagent": AddReagent(s, Require(e.Id, "grant_reagent.id"), e.Count ?? 1); return true;
            case "remove_reagent": AddReagent(s, Require(e.Id, "remove_reagent.id"), -(e.Count ?? 1)); return true;
            case "gain_secret": s.KnownSecrets.Add(Require(e.Id, "gain_secret.id")); return true;
            case "reveal_secret": s.KnownSecrets.Remove(Require(e.Id, "reveal_secret.id")); return true;
            case "patron_relationship": AddPatron(s, Require(e.Id, "patron_relationship.id"), e.Amount ?? 0); return true;
            case "unlock_ritual": s.UnlockedRituals.Add(Require(e.Id, "unlock_ritual.id")); return true;
            case "unlock_scheme": s.UnlockedSchemes.Add(Require(e.Id, "unlock_scheme.id")); return true;

            // Progression and flow.
            case "tier_change": s.Tier = Math.Max(0, s.Tier + (int)Math.Round(e.Amount ?? 0)); return true;
            case "queue_event": s.Queue.Add(new QueuedEvent { EventId = Require(e.Id, "queue_event.id"), Delay = e.Delay ?? 0 }); return true;
            case "end_game": s.IsGameOver = true; s.Ending = e.Ending; return true;

            default:
                return false; // Valid vocabulary; handler ships with its system (e.g. great_work_*).
        }
    }

    private static void AddResource(GameState s, string? key, double amount)
    {
        switch (Require(key, "resource.key"))
        {
            case ContentIds.Funds: s.Funds += amount; break;
            case ContentIds.Lore: s.Lore += amount; break;
            case ContentIds.Reagents: s.Reagents += amount; break;
            default: throw new ArgumentException($"Unknown resource '{key}'.");
        }
    }

    private static void AddAttention(GameState s, string? key, double amount)
    {
        switch (Require(key, "attention.key"))
        {
            case ContentIds.AttentionMundane: s.AttentionMundane += amount; break;
            case ContentIds.AttentionOccult: s.AttentionOccult += amount; break;
            default: throw new ArgumentException($"Unknown attention channel '{key}'.");
        }
    }

    private static InstitutionState InstitutionFor(GameState s, string? key)
    {
        string id = Require(key, "institution key");
        if (!s.Institutions.TryGetValue(id, out InstitutionState? inst))
        {
            inst = new InstitutionState();
            s.Institutions[id] = inst;
        }

        return inst;
    }

    private static void Recruit(GameState s, string? template, int count)
    {
        string baseId = string.IsNullOrEmpty(template) ? "initiate" : template;
        for (int i = 0; i < count; i++)
        {
            s.RecruitCounter++;
            s.Members.Add(new Member
            {
                Id = $"{baseId}_{s.RecruitCounter}",
                Status = MemberStatus.Active,
            });
        }
    }

    private static void AddRelationship(GameState s, BindingContext? ctx, Effect e)
    {
        string fromId = Bound(ctx, e.Scope).Id;
        string toId = Bound(ctx, e.TargetScope).Id;
        RelationshipKind kind = ParseKind(e.Kind);

        Relationship? rel = s.Relationships
            .FirstOrDefault(r => r.FromId == fromId && r.ToId == toId && r.Kind == kind);
        if (rel is null)
        {
            rel = new Relationship { FromId = fromId, ToId = toId, Kind = kind };
            s.Relationships.Add(rel);
        }

        rel.Value += e.Amount ?? 0;
    }

    private static void AddReagent(GameState s, string id, int delta)
    {
        int value = s.ReagentItems.GetValueOrDefault(id) + delta;
        if (value <= 0)
        {
            s.ReagentItems.Remove(id);
        }
        else
        {
            s.ReagentItems[id] = value;
        }
    }

    private static void AddPatron(GameState s, string id, double amount) =>
        s.PatronRelationships[id] = s.PatronRelationships.GetValueOrDefault(id) + amount;

    private static Member Bound(BindingContext? ctx, string? scope) =>
        ctx is not null && scope is not null && ctx.TryGet(scope, out Member? m) && m is not null
            ? m
            : throw new InvalidOperationException(
                $"Effect references scope '{scope}' which is not bound to a member.");

    private static MemberStatus ParseStatus(string? status) =>
        Enum.TryParse(status, ignoreCase: true, out MemberStatus parsed)
            ? parsed
            : throw new ArgumentException($"Unknown member status '{status}'.");

    private static RelationshipKind ParseKind(string? kind) =>
        Enum.TryParse(kind, ignoreCase: true, out RelationshipKind parsed)
            ? parsed
            : throw new ArgumentException($"Unknown relationship kind '{kind}'.");

    private static string Require(string? value, string what) =>
        string.IsNullOrEmpty(value)
            ? throw new ArgumentException($"Effect is missing required '{what}'.")
            : value;
}
