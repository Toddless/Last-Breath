namespace Core.Services
{
    using System;
    using Entity;

    /// <summary>
    /// Set-once holder for the player instance. The player is a scene node created by Godot,
    /// not by the container, so it registers itself here on _Ready; UI and services that need
    /// the player (ability book, parameters) resolve this instead of a scene lookup.
    /// </summary>
    public interface IPlayerAccessor
    {
        IPlayer? Player { get; }

        event Action<IPlayer>? PlayerChanged;

        void Set(IPlayer player);
    }
}
