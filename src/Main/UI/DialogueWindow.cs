namespace LastBreath.UI
{
    using Core.Data;
    using Core.Localization;
    using Core.Narrative.Dialogues;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// Dumb presenter over IDialogueService: one scrollable log accumulates the whole
    /// conversation (that IS the history), option buttons are rebuilt per node. Lines are
    /// appended once per node entry, so re-renders never duplicate text.
    /// </summary>
    public partial class DialogueWindow : PanelContainer, IWindow
    {
        private const string UID = "uid://bdlg4qst1awnd";

        [Export] private RichTextLabel? _log;
        [Export] private VBoxContainer? _options;
        private IDialogueService? _dialogue;
        private bool _closing;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void InjectServices(IGameServiceProvider provider)
        {
            GD.Print("Injected services to DialogueWindow");
            _dialogue = provider.GetService<IDialogueService>();
            _dialogue.Changed += Render;
            _dialogue.Ended += OnConversationEnded;
            Render();
        }

        public void Close()
        {
            EndConversation();
            QueueFree();
        }

        public override void _ExitTree()
        {
            if (_dialogue == null) return;
            _dialogue.Changed -= Render;
            _dialogue.Ended -= OnConversationEnded;
            EndConversation();
        }

        private void EndConversation()
        {
            if (_closing || _dialogue == null) return;
            _closing = true;
            _dialogue.End();
        }

        private void OnConversationEnded()
        {
            if (_closing) return;
            _closing = true;
            QueueFree();
        }

        private void Render()
        {
            if (_dialogue?.Current is not { } node) return;

            foreach (var line in node.Lines)
                AppendLine(SpeakerName(node, line), Localization.Localize(line.TextKey));

            RebuildOptions(node);
        }

        private void RebuildOptions(DialogueNodeView node)
        {
            if (_options == null) return;

            foreach (var child in _options.GetChildren())
                child.QueueFree();

            foreach (var option in node.Options)
            {
                var button = new Button
                {
                    Text = Localization.Localize(option.TextKey),
                    Disabled = !option.Enabled,
                    Alignment = HorizontalAlignment.Left,
                };
                button.Pressed += () => OnOptionPressed(option);
                _options.AddChild(button);
            }
        }

        private void OnOptionPressed(DialogueOptionView option)
        {
            AppendLine(Localization.Localize("UI_Dialogue_You"), Localization.Localize(option.TextKey));
            _dialogue?.Choose(option.Id);
        }

        private void AppendLine(string speaker, string text) => _log?.AppendText($"[b]{speaker}:[/b] {text}\n");

        private static string SpeakerName(DialogueNodeView node, DialogueLine line) =>
            Localization.Localize(line.Speaker == DialogueSpeaker.Player ? "UI_Dialogue_You" : node.NpcId);
    }
}
