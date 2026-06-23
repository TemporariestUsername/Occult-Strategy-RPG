using System.Text.Json.Nodes;

namespace PaleCommunion.Sim.Saves;

/// <summary>
/// One step in the save-migration pipeline. Each migration upgrades a save's raw
/// JSON from <see cref="FromVersion"/> to FromVersion + 1. Old saves must never
/// break silently (CLAUDE.md architecture law 4).
/// </summary>
public interface ISaveMigration
{
    int FromVersion { get; }

    void Apply(JsonObject root);
}
