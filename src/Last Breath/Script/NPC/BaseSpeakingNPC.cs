namespace Playground.Script.NPC
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Localization;
    using Playground.Resource.Quests;
    using Playground.Script.Helpers;
    using Playground.Script.QuestSystem;

    public partial class BaseSpeakingNPC : BaseNPC, ISpeaking
    {
        private readonly Dictionary<string, DialogueNode> _dialogs = [];
        private readonly List<Quest> _quests = [];
        public bool NpcTalking { get; set; } = true;
        public bool FirstTimeMeetPlayer = true;

        public List<Quest> Quests => _quests;
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

        protected override void LoadQuests()
        {
            var quests = ResourceLoader.Load<QuestCollection>(ResourcePath.QuestData);
            _quests.AddRange(quests.Quests.Where(quest => quest.NpcId == NpcId));
        }
    }
}
