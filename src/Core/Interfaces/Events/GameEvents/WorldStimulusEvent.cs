namespace Core.Interfaces.Events.GameEvents
{
    using Ai.World;

    /// <summary>
    /// A world stimulus broadcast on the global bus (e.g. a battle breaking out makes noise).
    /// Every NPC body routes it to its brain, which filters by its own hearing radius.
    /// </summary>
    public record WorldStimulusEvent(Stimulus Stimulus) : IGameEvent;
}
