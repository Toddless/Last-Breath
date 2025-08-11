namespace LastBreath.addons.Tools.TagGenerator
{
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class TagDefinition : Resource
    {
        [Export] public string Id {  get; set; } = string.Empty;
        [Export] public string DisplayName {  get; set; } = string.Empty;

        public string NormalizedId => TagUtils.Normalize(Id);

        public string EffectiveDisplay => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;
    }
}
