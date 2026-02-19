namespace LastBreath.Addons.Tools.TagGenerator
{
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class TagDefinition : Resource
    {
        [Export] public string Id {  get; set; } = string.Empty;
        [Export] public string DisplayName {  get; set; } = string.Empty;
    }
}
