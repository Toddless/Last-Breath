namespace LastBreath.addons.Tools.TagGenerator
{
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class TagRegistry : Resource
    {
        [Export] public string[] AllTags { get; set; } = [];
    }
}
