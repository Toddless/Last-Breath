namespace Core.Interfaces.Events.GameEvents
{
    /// <summary>
    /// The BattleDirector's playback state: true while recorded beats are being shown.
    /// The HUD uses it to close the player input window during animations
    /// (clicks/keys are blocked, tooltips stay available).
    /// </summary>
    public record PresentationStateChangedEvent(bool IsPlaying) : IBattleEvent;
}
