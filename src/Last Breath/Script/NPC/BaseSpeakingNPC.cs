namespace Playground.Script.NPC
{
    using System.Collections.Generic;
    using System.Text;
    using Godot;
    using Playground.Localization;
    using Playground.Resource;
    using Playground.Script.QuestSystem;

    public partial class BaseSpeakingNPC : BaseNPC, ISpeaking
    {
        private const string DialoguePath = "res://Resource/Dialogues/GuardianDialogues/guardianDialoguesData.tres";
        private readonly Dictionary<string, DialogueNode> _dialogs = [];
        private List<string> _quests = [];
        private string? _dialogueNodeId;
        public bool NpcTalking { get; set; } = true;
        public bool FirstTimeMeetPlayer = true;

        public List<string> Quests => _quests;
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;
        public string? FirstDialogueNode => _dialogueNodeId;

        protected override void SetDialogs()
        {
            var dialogueData = ResourceLoader.Load<DialogueData>(DialoguePath);
            if (dialogueData.Dialogs == null) return;
            foreach (var item in dialogueData.Dialogs)
            {
                _dialogs.Add(item.Key, item.Value);
            }
        }

        protected override void SetQuests()
        {
            _quests = QuestsTable.Instance.GetAllQuests(NpcId);
            foreach (var quest in _quests)
            {
                var questToSubscribe = QuestsTable.Instance.GetQuest(quest);
                if (questToSubscribe == null) continue;
                questToSubscribe.StatusChanged += OnQuestStatusChanged;
            }
        }

        protected override void SetFirstDialogueNode()
        {
            var fistDialogueNode = new StringBuilder();
            fistDialogueNode.Append(Name);
            fistDialogueNode.Append("FirstMeeting");
            _dialogueNodeId = fistDialogueNode.ToString();
        }

        private void OnQuestStatusChanged(string questId, QuestStatus status)
        {
            if (status != QuestStatus.Completed) return;
            var dialogue = QuestsTable.Instance.GetQuest(questId);
            if(dialogue == null) return;
            _dialogueNodeId = _dialogs[dialogue.DialogueIdOnComplete].DialogueId;
        }
    }
}
