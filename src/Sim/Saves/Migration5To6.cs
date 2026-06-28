using System.Text.Json.Nodes;

namespace PaleCommunion.Sim.Saves;

/// <summary>
/// Upgrades version 5 saves to version 6: introduces the psychological layer — per-member
/// Sanity and Law/Chaos Alignment, plus the order's own Alignment. Existing campaigns
/// backfill to sound minds (100) and Neutral (0) leanings.
/// </summary>
internal sealed class Migration5To6 : ISaveMigration
{
    public int FromVersion => 5;

    public void Apply(JsonObject root)
    {
        if (root["state"] is not JsonObject state)
        {
            state = new JsonObject();
            root["state"] = state;
        }

        state["alignment"] ??= 0;

        Backfill(state["members"] as JsonArray);
        Backfill(state["recruit_pool"] as JsonArray);
    }

    private static void Backfill(JsonArray? members)
    {
        if (members is null)
        {
            return;
        }

        foreach (JsonNode? node in members)
        {
            if (node is JsonObject member)
            {
                member["sanity"] ??= 100;
                member["alignment"] ??= 0;
            }
        }
    }
}
