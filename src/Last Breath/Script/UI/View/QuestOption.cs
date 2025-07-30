namespace LastBreath.Script.UI.View
{
    using System;
    using Godot;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.QuestSystem;
    using LastBreath.Resource.Quests;

    public partial class QuestOption : Button
    {
        private Quest? _quest;
        public event Action<string, Quest?>? QuestDetails;
        [Signal]
        public delegate void QuestSelectedEventHandler();

        public void Bind(Quest quest)
        {
            _quest = quest;
            this.Text = quest.NameKey?.Text;
            this.Pressed += QuestPressed;
            _quest.StatusChanged += QuestStatusChanged;
        }

        public bool IsMatch(string questId) => _quest?.Id == questId;

        public override void _ExitTree()
        {
            Unbind();
            base._ExitTree();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath.QuestOption);

        private void Unbind()
        {
            if (_quest != null)
            {
                _quest.StatusChanged -= QuestStatusChanged;
                _quest = null;
            }
            this.Pressed -= QuestPressed;
            QuestDetails = null;
        }

        private void QuestStatusChanged(string questId, QuestStatus status)
        {
            switch (status)
            {
                case QuestStatus.Progressing:
                    // code for changing status
                    break;
                case QuestStatus.Completed:
                    // code for changing status
                    break;
                case QuestStatus.Canceled:
                    // code for changing status
                    break;
                case QuestStatus.Failed:
                    // code for changing status
                    break;
                default:
                    break;
            }
        }

        private void QuestPressed()
        {
            // different description depends on quest status??
            QuestDetails?.Invoke(_quest?.DescriptionKey?.Text ?? string.Empty, _quest);
            EmitSignal(SignalName.QuestSelected);
        }
    }
}
