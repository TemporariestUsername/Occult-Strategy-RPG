using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class GreatWorkTests
{
    private static IRng Rng() => new SplitMix64Rng(0);

    private static List<Effect> Effects(string json) =>
        JsonSerializer.Deserialize<List<Effect>>(json, SimJson.Content)!;

    private static Condition Cond(string json) =>
        JsonSerializer.Deserialize<Condition>(json, SimJson.Content)!;

    private static EventCard Card(string json) =>
        JsonSerializer.Deserialize<EventCard>(json, SimJson.Content)!;

    [Fact]
    public void SetStep_Chooses_ThenAdvanceAccumulates()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"great_work_set_step\",\"id\":\"the_opening\",\"value\":1}]"), Rng());
        Assert.Equal("the_opening", s.ChosenGreatWork);
        Assert.Equal(1, s.GreatWorkStep);

        EffectEngine.Apply(s, Effects("[{\"type\":\"great_work_advance\",\"id\":\"the_opening\",\"steps\":2}]"), Rng());
        Assert.Equal(3, s.GreatWorkStep);
    }

    [Fact]
    public void Advance_WithoutId_UsesChosenWork()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"great_work_set_step\",\"id\":\"dominion\",\"value\":1}]"), Rng());
        EffectEngine.Apply(s, Effects("[{\"type\":\"great_work_advance\",\"steps\":1}]"), Rng());
        Assert.Equal("dominion", s.ChosenGreatWork);
        Assert.Equal(2, s.GreatWorkStep);
    }

    [Fact]
    public void ChoosingASecondWork_Throws_MutualExclusivity()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"great_work_set_step\",\"id\":\"the_opening\",\"value\":1}]"), Rng());
        Assert.Throws<ArgumentException>(
            () => EffectEngine.Apply(s, Effects("[{\"type\":\"great_work_set_step\",\"id\":\"dominion\",\"value\":1}]"), Rng()));
    }

    [Fact]
    public void Advance_NoId_NoneChosen_Throws()
    {
        var s = GameState.NewCampaign(1);
        Assert.Throws<ArgumentException>(
            () => EffectEngine.Apply(s, Effects("[{\"type\":\"great_work_advance\",\"steps\":1}]"), Rng()));
    }

    [Fact]
    public void Conditions_ChosenAndStep()
    {
        var s = GameState.NewCampaign(1);
        EffectEngine.Apply(s, Effects("[{\"type\":\"great_work_set_step\",\"id\":\"the_opening\",\"value\":3}]"), Rng());

        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"great_work_chosen\",\"id\":\"the_opening\"}"), Rng()));
        Assert.False(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"great_work_chosen\",\"id\":\"dominion\"}"), Rng()));
        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"great_work_chosen\"}"), Rng())); // any chosen
        Assert.True(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"great_work_step\",\"id\":\"the_opening\",\"op\":\"gte\",\"value\":3}"), Rng()));
        Assert.False(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"great_work_step\",\"id\":\"the_opening\",\"op\":\"gte\",\"value\":4}"), Rng()));

        // A different (unchosen) Great Work reads step 0.
        Assert.False(ConditionEvaluator.Evaluate(s, Cond("{\"type\":\"great_work_step\",\"id\":\"dominion\",\"op\":\"gte\",\"value\":1}"), Rng()));
    }

    [Fact]
    public void FullChain_ThroughResolver_ReachesEnding()
    {
        var s = GameState.NewCampaign(1);
        var rng = new SplitMix64Rng(7);

        // 1) Swear the oath -> commit to The Opening at step 1.
        EventCard oath = Card("{\"id\":\"gw.oath\",\"title\":\"Oath\",\"body\":\"b\",\"choices\":[{\"id\":\"swear\",\"outcome\":{\"effects\":[{\"type\":\"great_work_set_step\",\"id\":\"the_opening\",\"value\":1},{\"type\":\"order_corruption\",\"amount\":10}]}}]}");
        EventResolver.Begin(s, oath, rng);
        EventResolver.Choose(s, oath.Choices[0], rng, null);
        Assert.Equal("the_opening", s.ChosenGreatWork);
        Assert.Equal(1, s.GreatWorkStep);

        // 2) A card gated on the chosen work + step -> breach and end the game.
        EventCard breach = Card("{\"id\":\"gw.breach\",\"title\":\"Breach\",\"body\":\"b\",\"trigger\":{\"type\":\"pool\",\"conditions\":{\"type\":\"all_of\",\"conditions\":[{\"type\":\"great_work_chosen\",\"id\":\"the_opening\"},{\"type\":\"great_work_step\",\"id\":\"the_opening\",\"op\":\"gte\",\"value\":1}]}},\"choices\":[{\"id\":\"breach\",\"outcome\":{\"effects\":[{\"type\":\"great_work_advance\",\"id\":\"the_opening\",\"steps\":1},{\"type\":\"end_game\",\"ending\":\"the_opening\"}]}}]}");
        BindingResolution binding = EventResolver.Begin(s, breach, rng);
        Assert.True(EventResolver.IsEligible(s, breach, rng, binding.Context));

        ChoiceResolution res = EventResolver.Choose(s, breach.Choices[0], rng, binding.Context);

        Assert.True(res.Allowed);
        Assert.Equal(2, s.GreatWorkStep);
        Assert.True(s.IsGameOver);
        Assert.Equal("the_opening", s.Ending);
    }
}
