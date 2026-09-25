namespace Core.Events
{
    using System.Collections.Generic;
    using Godot;

    /// <summary>
    /// A skirmish round resolved: the winners "attack". Presentation hook — when the player is
    /// nearby, attack/hurt animations with random pauses play off this event (not built yet).
    /// </summary>
    public record NpcSkirmishRoundResolvedEvent(Vector2 Position, int Round, IReadOnlyList<string> RoundWinnerIds, IReadOnlyList<string> RoundLoserIds) : IGameEvent;

    /// <summary>The skirmish is over: the losers are already dead (their body lifecycles applied).</summary>
    public record NpcSkirmishEndedEvent(Vector2 Position, IReadOnlyList<string> WinnerIds, IReadOnlyList<string> LoserIds) : IGameEvent;
}
