using System.Text.Json.Nodes;

namespace PaleCommunion.Sim.Saves;

/// <summary>
/// Upgrades version 4 saves to version 5: backfills per-event cooldown bookkeeping for
/// the event pool / scheduler.
/// </summary>
internal sealed class Migration4To5 : ISaveMigration
{
    public int FromVersion => 4;

    public void Apply(JsonObject root)
    {
        if (root["state"] is not JsonObject state)
        {
            state = new JsonObject();
            root["state"] = state;
        }

        state["event_available_on"] ??= new JsonObject();
    }
}
