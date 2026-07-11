namespace Core.Events.GameEvents
{
    /// <summary>The defeated player finished lying in the world and stood up (10% HP rule).</summary>
    public record PlayerRevivedEvent : IGameEvent;
}
