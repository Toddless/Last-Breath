namespace Core.Events.GameEvents
{
    /// <summary>Outcome of the player's flee attempt — feedback for the battle log/UI.</summary>
    public record PlayerFleeResolvedEvent(bool Succeeded, float Chance) : IBattleEvent;
}
