namespace LastBreath.Script.UI
{
    using Godot;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using LastBreath.Script.UI.View;
    using LastBreath.Resource.Quests;

    public partial class QuestsWindow : Window, IInitializable
    {
        private const string UID = "uid://cqr8ohljxh8kt";
        [Export] private VBoxContainer? _mainQuests, _sideQuests;
        [Export] private RichTextLabel? _questDescription;
        [Export] private Label? _questStatus;

        public override void _Ready()
        {
            
        }

        public override void InjectServices(IGameServiceProvider provider)
        {

        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void AddQuests(Quest quest)
        {
            var questOption = QuestOption.Initialize().Instantiate<QuestOption>();
            questOption.Bind(quest);
            questOption.QuestDetails += OnQuestDetails;
            switch (quest.Type)
            {
                case QuestType.Main:
                    _mainQuests?.AddChild(questOption);
                    break;
                case QuestType.Side:
                    _sideQuests?.AddChild(questOption);
                    break;
            }
        }

        public void RemoveQuest(Quest quest)
        {
            switch (quest.Type)
            {
                case QuestType.Main:
                    FindAndRemoveChild(_mainQuests, quest);
                    break;
                case QuestType.Side:
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

        private void OnQuestDetails(string questDescription, Quest? quest)
        {
            _questDescription!.Text = questDescription;
            _questStatus!.Text = quest?.QuestStatus.ToString();
            // TODO: Show quest reward, progression
        }
    }
}
