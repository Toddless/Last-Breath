namespace Playground.Script.UI.View
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class DialogueWindow : Control
    {
        private VBoxContainer? _options;
        private RichTextLabel? _text;
        private TextureRect? _playerIcon;
        private Label? _questAdded;
        private Button? _skip, _moreSpeed, _lessSpeed, _default;
        private double _time = 0.015;
        private bool _canProceed = false;

        [Signal]
        public delegate void CanContinueEventHandler();

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
            _questAdded?.Hide();
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

        public void Clear()
        {
            if (_options == null) return;
            foreach (var item in _options.GetChildren())
            {
                item.QueueFree();
            }
        }

        private void SetEvents()
        {
            _moreSpeed!.Pressed += () => _time -= 0.01f;
            _lessSpeed!.Pressed += () => _time += 0.01f;
            _default!.Pressed += () => _time = 0.015f;
            _skip!.Pressed += () => _time = 0;
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
