using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

/// <summary>End-to-end resolution of the real shipped example cards (content/events/example_events.json).</summary>
public class EventResolutionTests
{
    private static List<EventCard> LoadExamples()
    {
        string path = TestPaths.Content("events/example_events.json");
        return JsonSerializer.Deserialize<List<EventCard>>(File.ReadAllText(path), SimJson.Content)!;
    }

    private static EventCard Card(string id) => LoadExamples().Single(c => c.Id == id);

    private static Member Infiltrator(string id, int guile, int infiltration)
    {
        var m = new Member { Id = id, Name = id };
        m.Attributes["Guile"] = guile;
        m.Skills["Infiltration"] = infiltration;
        return m;
    }

    [Fact]
    public void ExampleContent_DeserializesIntoCards()
    {
        List<EventCard> cards = LoadExamples();
        Assert.Equal(3, cards.Count);
        Assert.Contains(cards, c => c.Id == "onboarding.empty_chair");
        Assert.All(cards, c => Assert.NotEmpty(c.Choices));
    }

    [Fact]
    public void Onboarding_DeterministicChoice_AppliesEffects()
    {
        var s = GameState.NewCampaign(1);
        var rng = new SplitMix64Rng(42);
        EventCard card = Card("onboarding.empty_chair");

        BindingResolution binding = EventResolver.Begin(s, card, rng);
        Assert.True(binding.Success);

        Choice name = card.Choices.Single(c => c.Id == "name_the_order");
        double devotionBefore = s.Devotion;
        double loreBefore = s.Lore;

        ChoiceResolution res = EventResolver.Choose(s, name, rng, binding.Context);

        Assert.True(res.Allowed);
        Assert.Equal(devotionBefore + 5, s.Devotion);
        Assert.Equal(loreBefore + 2, s.Lore);
        Assert.True(s.Flags["order_named"]);
    }

    [Fact]
    public void Magistrate_BindsHighestGuileInfiltrator_AndResolvesCheck()
    {
        var s = GameState.NewCampaign(1);
        s.Members.Add(Infiltrator("weak", guile: 2, infiltration: 2));
        s.Members.Add(Infiltrator("strong", guile: 6, infiltration: 5));
        s.Institutions["state"].Influence = 10;

        EventCard card = Card("intrigue.magistrate_weakness");
        var rng = new SplitMix64Rng(2024);

        BindingResolution binding = EventResolver.Begin(s, card, rng);
        Assert.True(binding.Success);
        Assert.Equal("strong", binding.Context.Require("actor").Id); // prefer highest by Guile

        Choice blackmail = card.Choices.Single(c => c.Id == "blackmail");
        double stateBefore = s.Institutions["state"].Influence;

        ChoiceResolution res = EventResolver.Choose(s, blackmail, rng, binding.Context);

        Assert.True(res.Allowed);
        Assert.NotNull(res.Check);

        // The branch that fired must match the check outcome.
        if (res.Check!.Success)
        {
            Assert.True(s.Institutions["state"].Influence > stateBefore); // on_success raises State influence
            Assert.Contains("magistrate_second_name", s.KnownSecrets);    // and gains a secret
        }
        else
        {
            Assert.True(s.AttentionMundane > 0);                     // on_failure raises mundane attention
            Assert.Equal(MemberStatus.Injured, s.Members.Single(m => m.Id == "strong").Status);
        }
    }

    [Fact]
    public void Magistrate_RecruitChoice_GatedByTraitThenCost()
    {
        var s = GameState.NewCampaign(1);
        var actor = Infiltrator("a", guile: 3, infiltration: 2);
        s.Members.Add(actor);

        EventCard card = Card("intrigue.magistrate_weakness");
        var rng = new SplitMix64Rng(1);
        BindingResolution binding = EventResolver.Begin(s, card, rng);
        Assert.True(binding.Success);

        Choice recruit = card.Choices.Single(c => c.Id == "recruit_him");

        // Requires the 'persuasive' trait on the actor.
        Assert.False(EventResolver.IsAvailable(s, recruit, rng, binding.Context));

        actor.Traits.Add("persuasive");
        s.Funds = 40; // cost is 50 -> still unaffordable
        Assert.False(EventResolver.IsAvailable(s, recruit, rng, binding.Context));

        s.Funds = 100;
        Assert.True(EventResolver.IsAvailable(s, recruit, rng, binding.Context));

        int membersBefore = s.Members.Count;
        ChoiceResolution res = EventResolver.Choose(s, recruit, rng, binding.Context);

        Assert.True(res.Allowed);
        Assert.Equal(50, s.Funds);                        // 100 - 50 cost
        Assert.Equal(membersBefore + 1, s.Members.Count); // recruit effect added a member
        Assert.Equal("intrigue.first_favour", res.NextEvent);
        Assert.Contains(s.Queue, q => q.EventId == "intrigue.first_favour");
    }
}
