namespace Playground.Localization
{
    using Godot;
    using Godot.Collections;
    using Playground.Resource.Quests;

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
        [Export]
        public Array<Quest> Quests { get; set; } = [];
        [Export]
        public bool IsDialogMatterForQuest {  get; set; } = false;
    }
}
