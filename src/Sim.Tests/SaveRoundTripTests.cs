using PaleCommunion.Sim.Determinism;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.Saves;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Tests;

public class SaveRoundTripTests
{
    [Fact]
    public void RoundTrip_PreservesCoreState()
    {
        var s = GameState.NewCampaign(777);
        s.Funds = 250;
        s.Lore = 33;
        s.Veil = 42;
        s.Institutions["state"].Influence = 18;
        s.Flags["order_named"] = true;
        s.KnownSecrets.Add("rival_true_name");
        s.Members.Add(new Member { Id = "m1", Name = "Brother Ash", Status = MemberStatus.Injured, Corruption = 12 });

        GameState loaded = SaveSystem.Load(SaveSystem.Save(s));

        Assert.Equal("Boston", loaded.City);
        Assert.Equal(250, loaded.Funds);
        Assert.Equal(33, loaded.Lore);
        Assert.Equal(42, loaded.Veil);
        Assert.Equal(18, loaded.Institutions["state"].Influence);
        Assert.True(loaded.Flags["order_named"]);
        Assert.Contains("rival_true_name", loaded.KnownSecrets);
        Assert.Single(loaded.Members);
        Assert.Equal(MemberStatus.Injured, loaded.Members[0].Status);
        Assert.Equal("Brother Ash", loaded.Members[0].Name);
        Assert.Equal(12, loaded.Members[0].Corruption);
    }

    [Fact]
    public void RngState_SurvivesSaveLoad_KeepingDeterminism()
    {
        // Baseline: advance an RNG, note the checkpoint, then capture the next draws.
        var baseline = new SplitMix64Rng(2024);
        for (int i = 0; i < 4; i++)
        {
            baseline.NextUInt64();
        }

        ulong checkpoint = baseline.State;
        ulong[] expected = { baseline.NextUInt64(), baseline.NextUInt64() };

        // Persist the checkpoint in a save, reload, and resume from the stored state.
        var s = GameState.NewCampaign(0);
        s.RngState = checkpoint;
        GameState loaded = SaveSystem.Load(SaveSystem.Save(s));

        var resumed = new SplitMix64Rng(0) { State = loaded.RngState };
        Assert.Equal(expected[0], resumed.NextUInt64());
        Assert.Equal(expected[1], resumed.NextUInt64());
    }

    [Fact]
    public void LegacyVersionZeroSave_MigratesWithDefaults()
    {
        // A minimal pre-versioning save: version 0, sparse state.
        string legacy = "{\"version\":0,\"state\":{\"funds\":40,\"veil\":55}}";
        GameState loaded = SaveSystem.Load(legacy);

        Assert.Equal("Boston", loaded.City);   // backfilled by Migration0To1
        Assert.Equal(0, loaded.Reagents);        // backfilled
        Assert.Equal(0, loaded.OrderCorruption); // backfilled
        Assert.Equal(40, loaded.Funds);          // preserved
        Assert.Equal(55, loaded.Veil);           // preserved
        Assert.False(loaded.IsGameOver);         // v2 field, backfilled through 0 -> 1 -> 2 -> 3 -> 4
        Assert.Empty(loaded.ReagentItems);       // v2 field, backfilled
        Assert.Null(loaded.ChosenGreatWork);     // v3 field, backfilled
        Assert.Equal(0, loaded.GreatWorkStep);   // v3 field, backfilled
        Assert.Empty(loaded.ActiveSchemes);      // v4 field, backfilled
        Assert.Empty(loaded.RecruitPool);        // v4 field, backfilled
    }

    [Fact]
    public void FutureVersionSave_Throws()
    {
        Assert.Throws<NotSupportedException>(() => SaveSystem.Load("{\"version\":999,\"state\":{}}"));
    }

    [Fact]
    public void CurrentSave_DeclaresExpectedVersion()
    {
        string json = SaveSystem.Save(GameState.NewCampaign(1));
        Assert.Contains($"\"version\": {SaveSystem.CurrentVersion}", json);
    }
}
