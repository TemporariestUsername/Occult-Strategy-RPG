using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

public enum CheckOutcome
{
    CriticalSuccess,
    Success,
    Failure,
    CriticalFailure,
}

/// <summary>The full, inspectable result of a skill check.</summary>
public sealed class CheckResult
{
    public int DieA { get; init; }
    public int DieB { get; init; }
    public int StatBonus { get; init; }
    public int ModifierBonus { get; init; }
    public int Total { get; init; }
    public double Difficulty { get; init; }
    public double Margin { get; init; }
    public CheckOutcome Outcome { get; init; }

    public bool Success => Outcome is CheckOutcome.Success or CheckOutcome.CriticalSuccess;
    public bool Critical => Outcome is CheckOutcome.CriticalSuccess or CheckOutcome.CriticalFailure;
}

/// <summary>
/// Resolves a skill check. The math is intentionally simple and tunable — balance is
/// a design call: roll <b>2d6</b> and add the acting character's attribute + skill +
/// the sum of applicable modifiers, versus the difficulty. Natural <b>boxcars</b>
/// (6,6) are a critical success and <b>snake eyes</b> (1,1) a critical failure
/// regardless of the total; otherwise success is total &gt;= difficulty.
/// </summary>
public static class CheckResolver
{
    public static CheckResult Resolve(GameState state, Check check, BindingContext? context, IRng rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(check);
        ArgumentNullException.ThrowIfNull(rng);

        Member actor = Bound(context, check.Scope);

        int statBonus = 0;
        if (!string.IsNullOrEmpty(check.Attribute))
        {
            statBonus += actor.GetAttribute(check.Attribute);
        }

        if (!string.IsNullOrEmpty(check.Skill))
        {
            statBonus += actor.GetSkill(check.Skill);
        }

        int modifierBonus = 0;
        if (check.Modifiers is not null)
        {
            foreach (Modifier modifier in check.Modifiers)
            {
                if (modifier.When is null || ConditionEvaluator.Evaluate(state, modifier.When, rng, context))
                {
                    modifierBonus += (int)Math.Round(modifier.Value);
                }
            }
        }

        int a = rng.NextInt(1, 7);
        int b = rng.NextInt(1, 7);
        int total = a + b + statBonus + modifierBonus;

        CheckOutcome outcome;
        if (a == 6 && b == 6)
        {
            outcome = CheckOutcome.CriticalSuccess;
        }
        else if (a == 1 && b == 1)
        {
            outcome = CheckOutcome.CriticalFailure;
        }
        else
        {
            outcome = total >= check.Difficulty ? CheckOutcome.Success : CheckOutcome.Failure;
        }

        return new CheckResult
        {
            DieA = a,
            DieB = b,
            StatBonus = statBonus,
            ModifierBonus = modifierBonus,
            Total = total,
            Difficulty = check.Difficulty,
            Margin = total - check.Difficulty,
            Outcome = outcome,
        };
    }

    private static Member Bound(BindingContext? context, string scope) =>
        context is not null && context.TryGet(scope, out Member? m) && m is not null
            ? m
            : throw new InvalidOperationException($"Check scope '{scope}' is not bound to a member.");
}
