namespace Playground.Script.NPC
{
    using System.Collections.Generic;
    using System.Text;
    using Godot;
    using Playground.Localization;
    using Playground.Resource;
    using Playground.Resource.Quests;
    using Playground.Script.QuestSystem;

    public partial class BaseSpeakingNPC : BaseNPC
    {
        private const string DialoguePath = "res://Resources/Dialogues/GuardianDialogues/guardianDialoguesData.tres";
        private readonly Dictionary<string, DialogueNode> _dialogs = [];
        private List<string> _quests = [];
        private string _initialDialogueNodeId = string.Empty;
        public bool FirstTimeMeetPlayer { get; set; } = true;

        public List<string> Quests => _quests;
        public List<string> CompletedQuests { get; set; } = [];
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;
        public string InitialDialogueId => _initialDialogueNodeId;

        public void OnPlayerAcceptQuest(string questId)
        {
            if (!QuestsTable.Instance.TryGetElement(questId, out Quest? quest) || quest == null) return;
            quest.StatusChanged += OnQuestStatusChanged;
        }

        public void UpdateFirstDialogueNode()
        {
            var firstDialogueNode = new StringBuilder();
            firstDialogueNode.Append(Name);
            firstDialogueNode.Append("ReqularMeeting");
            _initialDialogueNodeId = firstDialogueNode.ToString();
        }

        protected override void SetDialogs()
        {
            var dialogueData = ResourceLoader.Load<DialogueData>(DialoguePath);
            if (dialogueData.Dialogs == null) return;
            foreach (var item in dialogueData.Dialogs)
            {
                _dialogs.Add(item.Key, item.Value);
            }
        }

        protected override void SetQuests() => _quests = QuestsTable.Instance.GetAllElements(NpcId);

        protected override void SetFirstDialogueNode()
        {
            var firstDialogueNode = new StringBuilder();
            firstDialogueNode.Append(Name);
            firstDialogueNode.Append("FirstMeeting");
            _initialDialogueNodeId = firstDialogueNode.ToString();
        }
        
        private void OnQuestStatusChanged(string questId, QuestStatus status)
        {
            // TODO: what if player can complete multiple quests?
            if (status != QuestStatus.Completed) return;
            CompletedQuests.Add(questId);
        }
    }
}
