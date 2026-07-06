namespace Core.Ai.World
{
    using Godot;

    /// <summary>A confirmed visual contact reported by the agent's senses.</summary>
    public readonly record struct TargetSighting(Vector2 Position);

    /// <summary>
    /// What the world brain requires from its body. Implemented by the Godot node (locomotion,
    /// senses); the brain itself stays pure C#. Battle initiation is NOT here — physical contact
    /// through the existing interaction area starts the fight, the brain only walks into it.
    /// </summary>
    public interface IWorldAgent
    {
        Vector2 Position { get; }

        /// <summary>Anchor of the NPC's life: spawn point; the leash and activities are relative to it.</summary>
        Vector2 HomePosition { get; }

        bool IsFighting { get; }

        /// <summary>Sets the current movement intent; safe to call every tick with the same destination.</summary>
        void MoveTo(Vector2 destination, float speed);

        void StopMoving();

        /// <summary>The hostile currently visible within the radius, if any (LoS is the adapter's concern).</summary>
        TargetSighting? GetSighting(float visionRadius);
    }
}
