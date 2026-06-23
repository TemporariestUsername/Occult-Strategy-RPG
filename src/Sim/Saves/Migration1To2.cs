using System.Text.Json.Nodes;

namespace PaleCommunion.Sim.Saves;

/// <summary>
/// Upgrades version 1 saves to version 2: backfills the collections added with the
/// character and event-resolution systems — itemised reagents, relationships, the
/// event queue, the recruit counter, and the game-over flag. Member sub-objects gain
/// their new attribute/skill/trait collections from default initialisers on load.
/// </summary>
internal sealed class Migration1To2 : ISaveMigration
{
    public int FromVersion => 1;

    public void Apply(JsonObject root)
    {
        if (root["state"] is not JsonObject state)
        {
            state = new JsonObject();
            root["state"] = state;
        }

        state["reagent_items"] ??= new JsonObject();
        state["relationships"] ??= new JsonArray();
        state["queue"] ??= new JsonArray();
        state["recruit_counter"] ??= 0;
        state["is_game_over"] ??= false;
    }
}
