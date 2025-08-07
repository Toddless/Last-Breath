namespace LastBreath.Script.Items.ItemData
{
    using Godot;
    using Godot.Collections;
    using Core.Enums;

    [GlobalClass]
    public partial class ItemResources : Resource
    {
        [Export] public Dictionary<Rarity, Texture2D> IconTexture { get; set; } = [];
        [Export] public Dictionary<Rarity, Texture2D> FullTexture { get; set; } = [];
        [Export] public Dictionary<Rarity, AudioStream> Sound { get; set; } = [];
    }
}
