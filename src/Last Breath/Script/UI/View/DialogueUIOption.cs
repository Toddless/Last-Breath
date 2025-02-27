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
            _option.Pressed += () => Option?.Invoke(_dialogueOption);
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath.DialogueOption);
    }
}
