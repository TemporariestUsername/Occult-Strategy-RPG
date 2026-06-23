namespace PaleCommunion.Sim.State;

/// <summary>An event scheduled to fire after <see cref="Delay"/> turns (via next_event or queue_event).</summary>
public sealed class QueuedEvent
{
    public string EventId { get; set; } = string.Empty;
    public int Delay { get; set; }
}
