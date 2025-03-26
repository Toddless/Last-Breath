namespace Playground.Script.UI.View
{
    using System;
    using Godot;
    using Playground.Resource.Quests;
    using Playground.Script.Helpers;
    using Playground.Script.QuestSystem;

    public partial class QuestOption : Button
    {
        private Quest? _quest;
        public event Action<string, Quest?>? QuestDetails;

        public void Bind(Quest quest)
        {
            _quest = quest;
            this.Text = quest.NameKey?.Text;
            this.Pressed += QuestSelected;
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
            this.Pressed -= QuestSelected;
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

        private void QuestSelected() => QuestDetails?.Invoke(_quest?.DescriptionKey?.Text ?? string.Empty, _quest);
    }
}
