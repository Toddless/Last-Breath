namespace Core.Entity.Silhouette
{
    /// <summary>A capsule shape placement in NPC-local coordinates: where its center sits and
    /// how big it is. Height is the Godot CapsuleShape2D total height (caps included).</summary>
    public readonly record struct CapsuleLayout(float CenterX, float CenterY, float Radius, float Height);

    /// <summary>A circle shape placement in NPC-local coordinates.</summary>
    public readonly record struct CircleLayout(float CenterX, float CenterY, float Radius);

    /// <summary>
    /// The full collision fit of one NPC scene. Which entries are present mirrors the scene:
    /// Main's body is a capsule and it has a dialogue circle, Battle's body is a circle and has
    /// no dialogue. The interaction zone (the Area2D that starts battles) exists in both.
    /// </summary>
    public readonly record struct NpcCollisionProfile(
        CapsuleLayout? BodyCapsule,
        CircleLayout? BodyCircle,
        CapsuleLayout Interaction,
        CircleLayout? Dialogue);
}
