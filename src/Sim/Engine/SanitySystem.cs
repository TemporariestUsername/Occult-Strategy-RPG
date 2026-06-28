using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>How a member comes apart when their mind reaches the floor.</summary>
public enum SanityBreakKind
{
    /// <summary>A lasting derangement — the member is Maddened but lives.</summary>
    Derangement,

    /// <summary>A violent fit: the order's cohesion suffers and a bond turns to rivalry.</summary>
    LashOut,

    /// <summary>The member flees the order and vanishes.</summary>
    Flight,

    /// <summary>The mind takes the body with it.</summary>
    Collapse,
}

/// <summary>A record of one member breaking, for the caller to narrate.</summary>
public readonly record struct SanityBreak(string MemberId, SanityBreakKind Kind);

/// <summary>
/// Resolves Sanity breaks. When an active member's Sanity reaches its floor the mind
/// gives way (the order's quiet Berserk — Shadow Hearts' SP married to CK3's stress).
/// Deterministic: every roll flows through the seeded RNG (law 3).
///
/// The floor, the post-break reprieve, the cohesion/bond costs, and the outcome weights
/// are PLACEHOLDER balance knobs (CLAUDE.md: tuning is a design decision) — revise freely.
/// What drains Sanity (occult work, ritual, the Pact) and when ResolveBreaks is called
/// in the turn are separate, later systems; this owns only the break itself.
/// </summary>
public static class SanitySystem
{
    public const double SanityFloor = 0;

    /// <summary>Sanity a survivor is left with, so one break is not an every-turn loop.</summary>
    public const double PostBreakSanity = 25;

    public const double LashOutDevotionHit = 8;
    public const double LashOutRelationshipHit = 20;

    // Weighted outcome table. Placeholder distribution — most breaks are survivable.
    private static readonly (SanityBreakKind Kind, double Weight)[] Outcomes =
    {
        (SanityBreakKind.Derangement, 0.45),
        (SanityBreakKind.LashOut, 0.30),
        (SanityBreakKind.Flight, 0.18),
        (SanityBreakKind.Collapse, 0.07),
    };

    /// <summary>
    /// Break every active member whose Sanity is at or below the floor, in roster order.
    /// Mutates state (status, devotion, relationships); returns what happened.
    /// </summary>
    public static IReadOnlyList<SanityBreak> ResolveBreaks(GameState state, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(rng);

        var breaks = new List<SanityBreak>();

        // Snapshot the roster: resolution changes status, but the at-risk set is fixed up front.
        foreach (Member member in state.Members.ToList())
        {
            if (member.Status != MemberStatus.Active || member.Sanity > SanityFloor)
            {
                continue;
            }

            SanityBreakKind kind = PickKind(rng);
            Apply(state, member, kind, rng);
            breaks.Add(new SanityBreak(member.Id, kind));
        }

        if (breaks.Count > 0)
        {
            state.Normalize();
        }

        return breaks;
    }

    private static void Apply(GameState state, Member member, SanityBreakKind kind, IRng rng)
    {
        switch (kind)
        {
            case SanityBreakKind.Derangement:
                member.Status = MemberStatus.Maddened;
                member.Sanity = PostBreakSanity;
                break;

            case SanityBreakKind.LashOut:
                member.Sanity = PostBreakSanity; // the fit passes; the damage is to those around them
                state.Devotion -= LashOutDevotionHit;
                Member? target = PickOther(state, member, rng);
                if (target is not null)
                {
                    // The one lashed at comes to resent the member.
                    state.AdjustRelationship(target.Id, member.Id, RelationshipKind.Rivalry, LashOutRelationshipHit);
                }

                break;

            case SanityBreakKind.Flight:
                member.Status = MemberStatus.Missing;
                break;

            case SanityBreakKind.Collapse:
                member.Status = MemberStatus.Dead;
                break;
        }
    }

    private static SanityBreakKind PickKind(IRng rng)
    {
        double total = 0;
        foreach ((SanityBreakKind _, double weight) in Outcomes)
        {
            total += weight;
        }

        double roll = rng.NextDouble() * total;
        double cumulative = 0;
        foreach ((SanityBreakKind kind, double weight) in Outcomes)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                return kind;
            }
        }

        return Outcomes[^1].Kind;
    }

    private static Member? PickOther(GameState state, Member self, IRng rng)
    {
        List<Member> others = state.Members
            .Where(m => m.Status == MemberStatus.Active && !ReferenceEquals(m, self))
            .ToList();

        return others.Count == 0 ? null : others[rng.NextInt(0, others.Count)];
    }
}
