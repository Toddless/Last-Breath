namespace LastBreath.Script.Items.ItemData
{
    using Godot;
    using Godot.Collections;
    using Contracts.Enums;
    using LastBreath.Localization;

    [GlobalClass]
    public partial class ItemResources : Resource
    {
        [Export] public Dictionary<Rarity, Texture2D> IconTexture { get; set; } = [];
        [Export] public Dictionary<Rarity, Texture2D> FullTexture { get; set; } = [];
        [Export] public Dictionary<Rarity, LocalizedString> Name { get; set; } = [];
        [Export] public Dictionary<Rarity, LocalizedString> Description { get; set; } = [];
        [Export] public Dictionary<Rarity, AudioStream> Sound { get; set; } = [];
    }
}
