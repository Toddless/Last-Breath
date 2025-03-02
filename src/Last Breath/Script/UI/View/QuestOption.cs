namespace Playground.Script.UI.View
{
    using System;
    using Godot;
    using Playground.Resource.Quests;
    using Playground.Script.Helpers;

    public partial class QuestOption : Button
    {
        private Quest? _quest;
        public event Action<string, Quest?>? QuestDescription;

        public void Bind(Quest quest)
        {
            _quest = quest;
            this.Text = quest.NameKey?.Text;
            this.Pressed += ThisPressed;
        }

        public bool IsMatch(string questId) => _quest?.Id == questId;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath.QuestOption);

        public override void _ExitTree()
        {
            Unbind();
            base._ExitTree();
        }

        private void Unbind()
        {
            if (_quest != null) _quest = null;
            this.Pressed -= ThisPressed;
            QuestDescription = null;
        }

        private void ThisPressed() => QuestDescription?.Invoke(_quest?.DescriptionKey?.Text ?? string.Empty, _quest);
    }
}
