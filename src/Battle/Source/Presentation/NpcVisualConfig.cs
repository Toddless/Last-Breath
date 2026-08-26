namespace Battle.Source.Presentation
{
    using Godot;

    /// <summary>
    /// One NPC's look: the SpriteFrames with its convention-named clips (Idle_*/Walk_*/
    /// Fight_Attack/Fight_Hurt/Dead — same names the AnimationsComponent plays). Partial art is
    /// legal: a name the set draws as a single frame or not at all is animated by tweens instead,
    /// so three static facings (Down/Up/Left) are a complete set. Scale multiplies the scene's base
    /// sprite scale (art is 480² like the placeholders — keep 1 for same-size art).
    /// </summary>
    [GlobalClass]
    public partial class NpcVisualConfig : Resource
    {
        [Export] public string NpcId { get; set; } = "";
        [Export] public SpriteFrames? Frames { get; set; }
        [Export] public float Scale { get; set; } = 1f;
    }
}
