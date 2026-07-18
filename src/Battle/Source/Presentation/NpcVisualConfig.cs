namespace Battle.Source.Presentation
{
    using Godot;

    /// <summary>
    /// One NPC's look: the SpriteFrames with its convention-named clips (Idle_*/Walk_*/
    /// Fight_Attack/Fight_Hurt/Dead — same names the AnimationsComponent plays). A clip
    /// missing from the set falls back to the component's missing-clip pause, so partial
    /// art is legal. Scale multiplies the scene's base sprite scale (art is 480² like the
    /// placeholders — keep 1 for same-size art).
    /// </summary>
    [GlobalClass]
    public partial class NpcVisualConfig : Resource
    {
        [Export] public string NpcId { get; set; } = "";
        [Export] public SpriteFrames? Frames { get; set; }
        [Export] public float Scale { get; set; } = 1f;
    }
}
