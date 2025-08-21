namespace LastBreath.Script.Items.ItemData
{
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class ItemMediaData : Resource
    {
        [Export] public Texture2D? IconTexture { get; set; }
        [Export] public Texture2D? FullTexture { get; set; }
        [Export] public AudioStream? Sound { get; set; }
    }
}
