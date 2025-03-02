namespace Playground.Script.UI.View
{
    using System;
    using Godot;
    using Playground.Localization;
    using Playground.Script.Helpers;

    public partial class DialogueUIOption : HBoxContainer
    {
        private Button? _option;
        private DialogueOption? _dialogueOption;

        public event Action<DialogueOption>? Option;

        public void Bind(DialogueOption option)
        {
            _option = GetNode<Button>(nameof(Button));
            _dialogueOption = option;
            _option!.Text = option.OptionName?.Text;
            _option.Pressed += OnOptionPressed;
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
            }
            Option = null;
        }

        public override void _ExitTree()
        {
            Unbind();
            base._ExitTree();
        }
    }
}
