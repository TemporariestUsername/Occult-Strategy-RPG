using System.Text.Json.Nodes;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Saves;

/// <summary>
/// Upgrades pre-1.0 prototype saves (version 0) to version 1: guarantees a state
/// object and backfills fields that version 1 expects but the earliest saves
/// lacked — the city, the reagents pool, order corruption, and the persisted RNG
/// state. Serves as the worked example the next migration is copied from.
/// </summary>
internal sealed class Migration0To1 : ISaveMigration
{
    public int FromVersion => 0;

    public void Apply(JsonObject root)
    {
        if (root["state"] is not JsonObject state)
        {
            state = new JsonObject();
            root["state"] = state;
        }

        state["city"] ??= GameState.DefaultCity;
        state["reagents"] ??= 0;
        state["order_corruption"] ??= 0;
        state["rng_state"] ??= 0;
    }
}
