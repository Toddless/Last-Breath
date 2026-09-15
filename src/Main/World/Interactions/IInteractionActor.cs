namespace LastBreath.World.Interactions
{
    using Godot;

    public interface IInteractionActor
    {
        /// <summary>Global point that interaction distance and the obstruction ray are measured from.</summary>
        Vector2 InteractionOrigin { get; }
    }
}
