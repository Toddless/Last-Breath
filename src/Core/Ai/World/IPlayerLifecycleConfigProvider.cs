namespace Core.Ai.World
{
    /// <summary>Source of the player death rules; the project side loads them from JSON.</summary>
    public interface IPlayerLifecycleConfigProvider
    {
        PlayerLifecycleConfig Config { get; }
    }
}
