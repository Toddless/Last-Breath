namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class QuestsMenu : Control
    {
        private VBoxContainer? _mainQuests, _sideQuests;
        private RichTextLabel? _questDescription;
        private HBoxContainer? _progression;

        public override void _Ready()
        {
            _mainQuests = (VBoxContainer?)NodeFinder.FindBFSCached(this, "MainQuests");
            _sideQuests = (VBoxContainer?)NodeFinder.FindBFSCached(this, "SideQuests");
            _questDescription = (RichTextLabel?)NodeFinder.FindBFSCached(this, "QuestDescription");

            NodeFinder.ClearCache();
        }
    }
}
