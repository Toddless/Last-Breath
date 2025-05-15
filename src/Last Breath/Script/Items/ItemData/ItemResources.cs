namespace Playground.Script.Items.ItemData
{
    using Godot;
    using Godot.Collections;
    using Playground.Localization;
    using Playground.Script.Enums;

    [GlobalClass]
    public partial class ItemResources : Resource
    {
        [Export] public Dictionary<GlobalRarity, Texture2D> IconTexture { get; set; } = [];
        [Export] public Dictionary<GlobalRarity, Texture2D> FullTexture { get; set; } = [];
        [Export] public Dictionary<GlobalRarity, LocalizedString> Name { get; set; } = [];
        [Export] public Dictionary<GlobalRarity, LocalizedString> Description { get; set; } = [];
        [Export] public Dictionary<GlobalRarity, AudioStream> Sound { get; set; } = [];
    }
}
