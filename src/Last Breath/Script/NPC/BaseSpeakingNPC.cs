namespace Playground.Script.NPC
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Localization;
    using Playground.Resource;

    public partial class BaseSpeakingNPC : BaseNPC, ISpeaking
    {
        private readonly Dictionary<string, DialogueNode> _dialogs = [];
        private List<string> _quests = [];
        public bool NpcTalking { get; set; } = true;
        public bool FirstTimeMeetPlayer = true;

        public List<string> Quests => _quests;
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;

        protected override void SetDialogs()
        {
            var dialogueData = ResourceLoader.Load<DialogueData>("res://Resource/Dialogues/GuardianDialogues/guardianDialoguesData.tres");
            if (dialogueData.Dialogs == null) return;
            foreach (var item in dialogueData.Dialogs)
            {
                _dialogs.Add(item.Key, item.Value);
            }
        }

        protected override void SetQuests() => _quests = QuestsTable.GetAllQuests(NpcId);
    }
}
