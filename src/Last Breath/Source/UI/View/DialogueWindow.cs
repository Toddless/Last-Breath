namespace LastBreath.Source.UI.View
{
    using Core.Interfaces.UI;
    using Godot;
    using Godot.Collections;

    public partial class DialogueWindow : Window, IInitializable
    {
        private const string UID = "";
        [Export] private VBoxContainer? _options, _buttonsContainer, _questsContainer;
        [Export] private RichTextLabel? _text;
        [Export] private TextureRect? _playerIcon;
        [Export] private Label? _questAdded;
        [Export] private Control? _questOptions;
        [Export] private Button? _skip, _moreSpeed, _lessSpeed, _default, _quests, _quit, _back, _accept, _completedQuests, _completeQuest;
        private double _time = 0.015;
        private bool _canProceed;

        public override void _Ready()
        {
        }


        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);


        public async void NewQuestAdded()
        {
            _questAdded?.Show();
            await ToSignal(GetTree().CreateTimer(1.5), "timeout");
            _questAdded?.Hide();
        }

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

    }
}
