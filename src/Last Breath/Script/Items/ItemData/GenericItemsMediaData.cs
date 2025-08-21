namespace LastBreath.Script.Items.ItemData
{
    using Godot;
    using Godot.Collections;

    [Tool]
    [GlobalClass]
    public partial class GenericItemsMediaData : Resource
    {
        [Export] public Dictionary<string, ItemMediaData> ItemMediaData { get; set; } = [];
    }
}
