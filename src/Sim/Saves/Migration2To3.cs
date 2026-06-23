using System.Text.Json.Nodes;

namespace PaleCommunion.Sim.Saves;

/// <summary>
/// Upgrades version 2 saves to version 3: backfills the Great Work step. The chosen
/// Great Work is absent (null) on old saves, which deserialises correctly without a
/// backfill.
/// </summary>
internal sealed class Migration2To3 : ISaveMigration
{
    public int FromVersion => 2;

    public void Apply(JsonObject root)
    {
        if (root["state"] is not JsonObject state)
        {
            state = new JsonObject();
            root["state"] = state;
        }

        state["great_work_step"] ??= 0;
    }
}
