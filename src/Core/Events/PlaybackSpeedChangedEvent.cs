namespace Core.Events
{
    /// <summary>Player-chosen battle playback speed multiplier; the BattleDirector scales its pacing by it.</summary>
    public record PlaybackSpeedChangedEvent(float Speed) : IBattleEvent;
}
