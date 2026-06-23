using PaleCommunion.Sim.Content;
using PaleCommunion.Sim.Model;
using PaleCommunion.Sim.State;

namespace PaleCommunion.Sim.Engine;

/// <summary>Shared bulk-resource helpers so cost handling lives in one place.</summary>
internal static class ResourceLedger
{
    public static double Value(GameState s, string resource) => resource switch
    {
        ContentIds.Funds => s.Funds,
        ContentIds.Lore => s.Lore,
        ContentIds.Reagents => s.Reagents,
        _ => throw new ArgumentException($"Unknown resource '{resource}'."),
    };

    public static void Add(GameState s, string resource, double amount)
    {
        switch (resource)
        {
            case ContentIds.Funds: s.Funds += amount; break;
            case ContentIds.Lore: s.Lore += amount; break;
            case ContentIds.Reagents: s.Reagents += amount; break;
            default: throw new ArgumentException($"Unknown resource '{resource}'.");
        }
    }

    public static bool CanAfford(GameState s, IEnumerable<Cost>? cost) =>
        cost is null || cost.All(c => Value(s, c.Resource) >= c.Amount);

    public static void Pay(GameState s, IEnumerable<Cost>? cost)
    {
        if (cost is null)
        {
            return;
        }

        foreach (Cost c in cost)
        {
            Add(s, c.Resource, -c.Amount);
        }
    }
}
