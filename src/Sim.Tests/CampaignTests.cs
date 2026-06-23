using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class CampaignTests
{
    [Fact]
    public void NewCampaign_BeginsInBoston_In1905()
    {
        var s = GameState.NewCampaign(12345);
        Assert.Equal("Boston", s.City);
        Assert.Equal(1905, s.Year);
        Assert.Equal(0, s.Tier);
    }

    [Fact]
    public void NewCampaign_HasAllSixInstitutions()
    {
        var s = GameState.NewCampaign(1);
        Assert.Equal(6, s.Institutions.Count);
        foreach (string id in ContentIds.Institutions)
        {
            Assert.True(s.Institutions.ContainsKey(id), $"missing institution {id}");
        }
    }

    [Fact]
    public void NewCampaign_StoresSeedForDeterminism()
    {
        var s = GameState.NewCampaign(98765);
        Assert.Equal(98765UL, s.RngState);
    }

    [Fact]
    public void NewCampaign_CanOverrideCity()
    {
        var s = GameState.NewCampaign(1, "Arkham");
        Assert.Equal("Arkham", s.City);
    }
}
