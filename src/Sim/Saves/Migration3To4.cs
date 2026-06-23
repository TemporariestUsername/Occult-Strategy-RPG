using System.Text.Json.Nodes;

namespace PaleCommunion.Sim.Saves;

/// <summary>
/// Upgrades version 3 saves to version 4: backfills the action-economy collections —
/// active schemes, the recruit pool, and per-scheme cooldown bookkeeping.
/// </summary>
internal sealed class Migration3To4 : ISaveMigration
{
    public int FromVersion => 3;

    public void Apply(JsonObject root)
    {
        if (root["state"] is not JsonObject state)
        {
            state = new JsonObject();
            root["state"] = state;
        }

        state["active_schemes"] ??= new JsonArray();
        state["recruit_pool"] ??= new JsonArray();
        state["scheme_available_on"] ??= new JsonObject();
    }
}
