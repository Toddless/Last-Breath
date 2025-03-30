namespace Playground.Script.UI
{
    using System.Linq;
    using Godot;
    using Playground.Resource.Quests;
    using Playground.Script.Helpers;
    using Playground.Script.UI.View;

    public partial class QuestsMenu : Control
    {
        private VBoxContainer? _mainQuests, _sideQuests;
        private RichTextLabel? _questDescription;
        private HBoxContainer? _progression;
        private Label? _questStatus;

        public override void _Ready()
        {
            _mainQuests = (VBoxContainer?)NodeFinder.FindBFSCached(this, "MainQuests");
            _sideQuests = (VBoxContainer?)NodeFinder.FindBFSCached(this, "SideQuests");
            _questDescription = (RichTextLabel?)NodeFinder.FindBFSCached(this, "QuestDescription");
            _questStatus = (Label?)NodeFinder.FindBFSCached(this, "Status");
            Hidden += QuestMenuHidden;
            NodeFinder.ClearCache();
        }

        public void AddQuests(Quest quest)
        {
            var questOption = QuestOption.Initialize().Instantiate<QuestOption>();
            questOption.Bind(quest);
            questOption.QuestDetails += OnQuestDetails;
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

        public void RemoveQuest(Quest quest)
        {
            switch (quest.Type)
            {
                case Enums.QuestType.Main:
                    FindAndRemoveChild(_mainQuests,quest);
                    break;
                case Enums.QuestType.Side:
                    FindAndRemoveChild(_sideQuests, quest);
                    break;
            }
        }

        private void FindAndRemoveChild(VBoxContainer? questsContainer, Quest quest)
        {
            foreach (var item in questsContainer!.GetChildren().Cast<QuestOption>().Where(item => item.IsMatch(quest.Id)))
            {
                item.QuestDetails -= OnQuestDetails;
                questsContainer.RemoveChild(item);
            }
        }


        // TODO: Decide later if i wanna clear description after hiding menu
        private void QuestMenuHidden()
        {
            _questDescription!.Text = string.Empty;
            _questStatus!.Text = string.Empty;
        }

        private void OnQuestDetails(string questDescription, Quest? quest)
        {
            _questDescription!.Text = questDescription;
            _questStatus!.Text = quest?.QuestStatus.ToString();
            // TODO: Show quest reward, progression
        }
    }
}
