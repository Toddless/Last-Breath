namespace Core.Events.GameEvents
{
    using Entity;

    /// <summary>A world NPC reached the ongoing battle (via the battle-site marker) and wants in.
    /// The side is a parameter: future companions join the player's group the same way.</summary>
    public record BattleJoinRequestEvent(IFightable Fighter, bool AlliedWithPlayer) : IGameEvent;
}
