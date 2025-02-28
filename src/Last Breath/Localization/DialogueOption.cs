namespace Playground.Localization
{
    using Godot;

    [GlobalClass]
    public partial class DialogueOption : Resource
    {
        [Export]
        public LocalizedString? OptionName { set; get; }

        [Export]
        public string TargetNode { get; set; } = string.Empty;

        [Export]
        public int RelationEffect { get; set; } = 0;
        [Export]
        public bool UsePlayerSource { get; set; } = true;
    }
}
