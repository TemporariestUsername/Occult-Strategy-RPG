using System.Text.Json;
using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Engine;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class BindingResolutionTests
{
    private static IRng Rng() => new SplitMix64Rng(5);

    private static Dictionary<string, Selector> Bindings(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, Selector>>(json, SimJson.Content)!;

    private static Member Infiltrator(string id, int guile, int infiltration)
    {
        var m = new Member { Id = id, Name = id };
        m.Attributes["Guile"] = guile;
        m.Skills["Infiltration"] = infiltration;
        return m;
    }

    [Fact]
    public void PrefersHighestByStat_AmongRequireMatches()
    {
        var s = GameState.NewCampaign(1);
        s.Members.Add(Infiltrator("low", guile: 2, infiltration: 1));
        s.Members.Add(Infiltrator("high", guile: 5, infiltration: 1));
        s.Members.Add(new Member { Id = "nospy", Name = "nospy" }); // no Infiltration -> excluded

        string json = "{\"actor\":{\"from\":\"member\",\"require\":[{\"type\":\"scope_skill\",\"key\":\"Infiltration\"}],\"prefer\":\"highest\",\"by\":\"Guile\"}}";
        BindingResolution r = BindingResolver.Resolve(s, Bindings(json), Rng());

        Assert.True(r.Success);
        Assert.Equal("high", r.Context.Require("actor").Id);
    }

    [Fact]
    public void NonOptionalBinding_WithNoCandidate_Disqualifies()
    {
        var s = GameState.NewCampaign(1); // no members
        string json = "{\"actor\":{\"from\":\"member\",\"require\":[{\"type\":\"scope_skill\",\"key\":\"Lore\"}]}}";
        BindingResolution r = BindingResolver.Resolve(s, Bindings(json), Rng());

        Assert.False(r.Success);
        Assert.Contains("actor", r.Unfilled);
        Assert.False(r.Context.Has("actor"));
    }

    [Fact]
    public void OptionalBinding_WithNoCandidate_StillSucceeds()
    {
        var s = GameState.NewCampaign(1);
        string json = "{\"rival\":{\"from\":\"member\",\"optional\":true,\"require\":[{\"type\":\"scope_has_trait\",\"id\":\"traitorous\"}]}}";
        BindingResolution r = BindingResolver.Resolve(s, Bindings(json), Rng());

        Assert.True(r.Success);
        Assert.False(r.Context.Has("rival"));
    }

    [Fact]
    public void UnmodelledPool_ResolvesEmpty()
    {
        var s = GameState.NewCampaign(1);
        s.Members.Add(Infiltrator("m", 3, 3));
        // rival_order is not modelled yet -> no candidates -> non-optional binding fails.
        string json = "{\"rival\":{\"from\":\"rival_order\"}}";
        BindingResolution r = BindingResolver.Resolve(s, Bindings(json), Rng());

        Assert.False(r.Success);
    }
}
