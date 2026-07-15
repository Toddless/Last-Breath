namespace Core.Events
{
    /// <summary>The player tries to flee (HUD button). Not guaranteed: the arena rolls the chance
    /// against the toughest standing enemy; failure costs the turn.</summary>
    public record PlayerFleeAttemptEvent : IBattleEvent;
}
