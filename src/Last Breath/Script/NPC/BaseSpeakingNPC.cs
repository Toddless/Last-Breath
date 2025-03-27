namespace Playground.Script.NPC
{
    using System.Collections.Generic;
    using System.Text;
    using Godot;
    using Playground.Localization;
    using Playground.Resource;
    using Playground.Resource.Quests;
    using Playground.Script.QuestSystem;

    public partial class BaseSpeakingNPC : BaseNPC, ISpeaking
    {
        private const string DialoguePath = "res://Resource/Dialogues/GuardianDialogues/guardianDialoguesData.tres";
        private readonly Dictionary<string, DialogueNode> _dialogs = [];
        private List<string> _quests = [];
        private string _initialDialogueNodeId = string.Empty;
        public bool NpcTalking { get; set; } = true;
        public bool FirstTimeMeetPlayer = true;

        public List<string> Quests => _quests;
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;
        public string InitialDialogueId => _initialDialogueNodeId;

        public void OnPlayerAcceptQuest(string questId)
        {
            if (!QuestsTable.Instance.TryGetElement(NpcId, out Quest? quest) || quest == null) return;
            quest.StatusChanged += OnQuestStatusChanged;
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
            var fistDialogueNode = new StringBuilder();
            fistDialogueNode.Append(Name);
            fistDialogueNode.Append("FirstMeeting");
            _initialDialogueNodeId = fistDialogueNode.ToString();
        }

        private void OnQuestStatusChanged(string questId, QuestStatus status)
        {
            // TODO: what if player can complete multiple quests?
            if (status != QuestStatus.Completed) return;
            var quest = QuestsTable.Instance.GetValue(questId);
            if (quest == null) return;
            _initialDialogueNodeId = quest.DialogueIdOnComplete;
        }
    }
}
