using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

/// <summary>Loads the committed example scheme/ritual content and runs it end-to-end.</summary>
public class CommittedContentTests
{
    private static List<Scheme> Schemes() =>
        JsonSerializer.Deserialize<List<Scheme>>(
            File.ReadAllText(TestPaths.Content("schemes/example_schemes.json")), SimJson.Content)!;

    private static List<Ritual> Rituals() =>
        JsonSerializer.Deserialize<List<Ritual>>(
            File.ReadAllText(TestPaths.Content("rituals/example_rituals.json")), SimJson.Content)!;

    [Fact]
    public void ExampleSchemes_LoadAndResolveThroughTheTurnLoop()
    {
        List<Scheme> schemes = Schemes();
        Scheme infiltrate = schemes.Single(s => s.Id == "intrigue.infiltrate_police");
        Scheme transcribe = schemes.Single(s => s.Id == "sanctum.transcribe_lore");
        Dictionary<string, Scheme> catalog = schemes.ToDictionary(s => s.Id);

        var s = GameState.NewCampaign(1);
        var spy = new Member { Id = "spy", Name = "Spy" };
        spy.Attributes["Guile"] = 4;
        spy.Skills["Infiltration"] = 4;
        s.Members.Add(spy);
        var rng = new SplitMix64Rng(7);

        Assert.True(SchemeService.Start(s, infiltrate, new Dictionary<string, string> { ["actor"] = "spy" }, rng).Started);
        Assert.Equal(80, s.Funds); // 100 - 20 cost

        Assert.Empty(TurnSystem.Advance(s, catalog, rng).ResolvedSchemes);
        Assert.Empty(TurnSystem.Advance(s, catalog, rng).ResolvedSchemes);
        Assert.Single(TurnSystem.Advance(s, catalog, rng).ResolvedSchemes); // duration 3

        // The no-binding scheme resolves to +5 lore after its two turns.
        double loreBefore = s.Lore;
        Assert.True(SchemeService.Start(s, transcribe, new Dictionary<string, string>(), rng).Started);
        TurnSystem.Advance(s, catalog, rng);
        TurnSystem.Advance(s, catalog, rng);
        Assert.Equal(loreBefore + 5, s.Lore);
    }

    [Fact]
    public void ExampleRitual_LoadsAndPerforms()
    {
        Ritual borrowed = Rituals().Single(r => r.Id == "the_borrowed_name");

        var s = GameState.NewCampaign(1);
        s.Lore = 30;
        s.ReagentItems["graveyard_dirt"] = 1;
        s.UnlockedRituals.Add("the_borrowed_name");
        var adept = new Member { Id = "adept", Name = "Adept" };
        adept.Attributes["Will"] = 4;
        adept.Skills["Ritual"] = 4;
        s.Members.Add(adept);

        RitualResult res = RitualService.Perform(
            s, borrowed, new Dictionary<string, string> { ["actor"] = "adept" }, new SplitMix64Rng(2024));

        Assert.True(res.Performed);
        Assert.Equal(5, s.OrderCorruption);                         // on_perform inherent price
        Assert.False(s.ReagentItems.ContainsKey("graveyard_dirt")); // reagent consumed
        Assert.True(s.AttentionOccult >= 5);
    }
}
