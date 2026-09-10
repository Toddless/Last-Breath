namespace Core.Ai.World
{
    using Godot;

    public enum StimulusType : byte
    {
        /// <summary>Something audible worth investigating (a scuffle nearby, a door, a shout).</summary>
        Noise,

        /// <summary>Direct danger signal (an ally under attack) — treated as noise for now, reserved for fleeing.</summary>
        Danger
    }

    /// <summary>A world event an NPC can react to without direct line of sight.</summary>
    public record Stimulus(StimulusType Type, Vector2 Position, ulong SpaceId = 0);
}
