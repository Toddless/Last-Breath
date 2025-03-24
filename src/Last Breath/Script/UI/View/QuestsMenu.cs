namespace Playground.Script.UI
{
    using Godot;
    using Playground.Resource.Quests;
    using Playground.Script.Helpers;
    using Playground.Script.UI.View;

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
            Hidden += QuestMenuHidden;
            NodeFinder.ClearCache();
        }


        public void AddQuests(Quest quest)
        {
            var questOption = QuestOption.Initialize().Instantiate<QuestOption>();
            questOption.Bind(quest);
            questOption.QuestDescription += OnDescription;
            switch (quest.Type)
            {
                case Enums.QuestType.Main:
                    _mainQuests?.AddChild(questOption);
                    break;
                case Enums.QuestType.Side:
                    _sideQuests?.AddChild(questOption);
                    break;
            }
        }

        // TODO: Decide later if i wanna clear description after menu is hidden
        private void QuestMenuHidden() => _questDescription!.Text = string.Empty;

        private void OnDescription(string arg1, Quest? quest)
        {
            _questDescription!.Text = arg1;
            // TODO: Show quest reward, progression
        }
    }
}
