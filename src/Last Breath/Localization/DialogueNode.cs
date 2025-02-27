namespace Playground.Localization
{
    using Godot;
    using Godot.Collections;

    [GlobalClass]
    public partial class DialogueNode : Resource
    {
        [Export]
        public Array<LocalizedString> Texts { get; set; } = [];
        [Export]
        public Array<DialogueOption> Options { get; set; } = [];
        [Export]
        public string DialogueId { get; set; } = string.Empty;
        [Export]
        public bool ReturnToPrevious { get; set; } = false;
    }
}
