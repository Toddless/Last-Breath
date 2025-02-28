namespace Playground.Script.UI.View
{
    using System;
    using Godot;
    using Playground.Localization;
    using Playground.Resource.Quests;
    using Playground.Script.Helpers;

    public partial class DialogueUIOption : HBoxContainer
    {
        private Button? _option;
        private DialogueOption? _dialogueOption;
        private Quest? _quest;

        public event Action<DialogueOption>? Option;
        public event Action<Quest>? Quest;

        public void Bind(DialogueOption option)
        {
            _option = GetNode<Button>(nameof(Button));
            _dialogueOption = option;
            _option!.Text = option.OptionName?.Text;
            _option.Pressed += OnOptionPressed;
        }

        public void BindQuest(Quest quest)
        {
            _option = GetNode<Button>(nameof(Button));
            _option!.Text = quest.DescriptionKey?.Text;
            _option.Pressed += OnQuestOptionPressed;
        }

        private void OnQuestOptionPressed()
        {
            if (_quest == null) return;
            Quest?.Invoke(_quest);
        }
        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath.DialogueOption);

        private void OnOptionPressed()
        {
            if (_dialogueOption == null) return;
            Option?.Invoke(_dialogueOption);
        }

        private void Unbind()
        {
            if (_option != null)
            {
                _option.Pressed -= OnOptionPressed;
                _option.Pressed -= OnQuestOptionPressed;
            }
            Option = null;
            Quest = null;
        }

        public override void _ExitTree()
        {
            Unbind();
            base._ExitTree();
        }

    }
}
