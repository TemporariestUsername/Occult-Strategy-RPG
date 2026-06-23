using System.Text.Json;
using System.Text.Json.Nodes;
using PaleCommunion.Sim.Serialization;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Saves;

/// <summary>
/// Versioned save/load. A save is <c>{ "version": N, "state": { ... } }</c>. On
/// load, registered migrations run in order until the document reaches
/// <see cref="CurrentVersion"/>, then the state is deserialized. Determinism is
/// preserved because the RNG stream position lives inside the state.
/// </summary>
public static class SaveSystem
{
    /// <summary>Current on-disk save format version. Bump when the state shape changes.</summary>
    public const int CurrentVersion = 1;

    private static readonly IReadOnlyList<ISaveMigration> Migrations = new ISaveMigration[]
    {
        new Migration0To1(),
    };

    public static string Save(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var doc = new JsonObject
        {
            ["version"] = CurrentVersion,
            ["state"] = JsonSerializer.SerializeToNode(state, SimJson.Save),
        };
        return doc.ToJsonString(SimJson.Save);
    }

    public static GameState Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Save payload is empty.", nameof(json));
        }

        JsonNode root = JsonNode.Parse(json)
            ?? throw new FormatException("Save payload is not valid JSON.");
        JsonObject obj = root.AsObject();

        int version = obj.TryGetPropertyValue("version", out JsonNode? versionNode) && versionNode is not null
            ? versionNode.GetValue<int>()
            : 0;

        if (version > CurrentVersion)
        {
            throw new NotSupportedException(
                $"Save version {version} is newer than this build supports ({CurrentVersion}). " +
                "Update the game to load it.");
        }

        while (version < CurrentVersion)
        {
            ISaveMigration migration = Migrations.FirstOrDefault(m => m.FromVersion == version)
                ?? throw new NotSupportedException(
                    $"No migration registered from save version {version}.");
            migration.Apply(obj);
            version++;
            obj["version"] = version;
        }

        JsonNode stateNode = obj["state"]
            ?? throw new FormatException("Save is missing its 'state' object.");
        GameState state = stateNode.Deserialize<GameState>(SimJson.Save)
            ?? throw new FormatException("Save 'state' could not be read.");

        state.Normalize();
        return state;
    }
}
