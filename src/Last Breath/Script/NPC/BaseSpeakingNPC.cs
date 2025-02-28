namespace Playground.Script.NPC
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Localization;

    public partial class BaseSpeakingNPC : BaseNPC, ISpeaking
    {
        private readonly Dictionary<string, DialogueNode> _dialogs = [];

        public bool NpcTalking { get; set; } = false;
        public bool FirstTimeMeetPlayer = true;

        public Dictionary<string, DialogueNode> Dialogs => _dialogs;

        protected void SetDialogs(string path)
        {
            var dialogueData = ResourceLoader.Load<DialogueData>(path);
            if (dialogueData.Dialogs == null) return;
            foreach (var item in dialogueData.Dialogs)
            {
                _dialogs.Add(item.Key, item.Value);
            }
        }
    }
}
