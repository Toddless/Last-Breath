namespace Playground.Script.UI.View
{
    using Godot;
    using Godot.Collections;
    using Playground.Script.Helpers;

    public partial class DialogueWindow : Control
    {
        private VBoxContainer? _options, _buttonsContainer, _questsContainer;
        private RichTextLabel? _text;
        private TextureRect? _playerIcon;
        private Label? _questAdded;
        private Control? _questOptions;
        private Button? _skip, _moreSpeed, _lessSpeed, _default, _quests, _quit, _back, _accept, _completedQuests, _completeQuest;
        private double _time = 0.015;
        private bool _canProceed = false;

        [Signal]
        public delegate void CanContinueEventHandler();
        [Signal]
        public delegate void QuitPressedEventHandler();
        [Signal]
        public delegate void QuestsPressedEventHandler();
        [Signal]
        public delegate void CloseDialogueWindowEventHandler();
        [Signal]
        public delegate void AcceptPressedEventHandler();
        [Signal]
        public delegate void BackPressedEventHandler();
        [Signal]
        public delegate void CompletedQuestsPressedEventHandler();
        [Signal]
        public delegate void QuestCompletedEventHandler();

        public override void _Ready()
        {
            _options = (VBoxContainer?)NodeFinder.FindBFSCached(this, "Options");
            _text = (RichTextLabel?)NodeFinder.FindBFSCached(this, "Text");
            _playerIcon = (TextureRect?)NodeFinder.FindBFSCached(this, "Avatar");
            _skip = (Button?)NodeFinder.FindBFSCached(this, "Skip");
            _moreSpeed = (Button?)NodeFinder.FindBFSCached(this, "MoreSpeed");
            _lessSpeed = (Button?)NodeFinder.FindBFSCached(this, "LessSpeed");
            _default = (Button?)NodeFinder.FindBFSCached(this, "Default");
            _questAdded = (Label?)NodeFinder.FindBFSCached(this, "Label");

            _buttonsContainer = (VBoxContainer?)NodeFinder.FindBFSCached(this, "ButtonsContainer");
            _quests = (Button?)NodeFinder.FindBFSCached(this, "QuestsBtn");
            _quit = (Button?)NodeFinder.FindBFSCached(this, "Quit");
            _questsContainer = (VBoxContainer?)NodeFinder.FindBFSCached(this, "Quests");
            _back = (Button?)NodeFinder.FindBFSCached(this, "Back");
            _accept = (Button?)NodeFinder.FindBFSCached(this, "Accept");
            _completedQuests = (Button?)NodeFinder.FindBFSCached(this, "CompletedQuests");
            _completeQuest = (Button?)NodeFinder.FindBFSCached(this, "CompleteQuest");

            _questOptions = (Control?)NodeFinder.FindBFSCached(this, "QuestOptionsElement");
            _questAdded?.Hide();
            _buttonsContainer?.Hide();
            SetEvents();

            NodeFinder.ClearCache();
        }

        public async void NewQuestAdded()
        {
            _questAdded?.Show();
            await ToSignal(GetTree().CreateTimer(1.5), "timeout");
            _questAdded?.Hide();
        }

        public void SetAvatar(Texture2D icon) => _playerIcon!.Texture = icon;
        public void SetOptions(Label option) => _options?.AddChild(option);

        public void ShowMainButtons() => _buttonsContainer?.Show();
        public void HideMainButtons() => _buttonsContainer?.Hide();
        public void ShowQuests() => _questsContainer?.Show();
        public void HideQuests() => _questsContainer?.Hide();
        public void ShowBackButton() => _back?.Show();
        public void HideBackButton() => _back?.Hide();
        public void ShowAcceptButton() => _accept?.Show();
        public void HideAcceptButton() => _accept?.Hide();
        public void ShowCompleteQuestButton() => _completeQuest?.Show();
        public void HideCompleteQuestButton() => _completeQuest?.Hide();

        public Array<Node> GetQuests() => _questsContainer?.GetChildren() ?? [];
        public async void UpdateText(string text)
        {
            _options?.Hide();
            _text!.Text = "";
            foreach (char c in text)
            {
                _text.Text += c;
                await ToSignal(GetTree().CreateTimer(_time), "timeout");
            }
            _canProceed = true;
            _options?.Show();
        }

        public void AddOption(DialogueUIOption option) => _options?.AddChild(option);

        public void AddQuest(QuestOption quest) => _questsContainer?.AddChild(quest);

        public void Clear()
        {
            if (_options == null) return;
            foreach (var item in _options.GetChildren())
            {
                item.QueueFree();
            }
        }

        public void ClearText() => _text?.Clear();

        public void CloseWindow() => EmitSignal(SignalName.CloseDialogueWindow);

        private void SetEvents()
        {
            _moreSpeed!.Pressed += () => _time -= 0.01f;
            _lessSpeed!.Pressed += () => _time += 0.01f;
            _default!.Pressed += () => _time = 0.015f;
            _skip!.Pressed += () => _time = 0;
            _quit!.Pressed += () => EmitSignal(SignalName.QuitPressed);
            _quests!.Pressed += () => EmitSignal(SignalName.QuestsPressed);
            _accept!.Pressed += () => EmitSignal(SignalName.AcceptPressed);
            _back!.Pressed += () => EmitSignal(SignalName.BackPressed);
            _completedQuests!.Pressed += () => EmitSignal(SignalName.CompletedQuestsPressed);
            _completeQuest!.Pressed += () => EmitSignal(SignalName.QuestCompleted);
        }

        private void OnTextClicked(InputEvent @event)
        {
            if (_canProceed && @event.IsActionPressed("LMB"))
            {
                EmitSignal(SignalName.CanContinue);
                _canProceed = false;
            }
        }
    }
}
