namespace Playground.Script.UI.View
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class DialogueWindow : Control
    {
        private VBoxContainer? _options;
        private RichTextLabel? _text;
        private TextureRect? _playerIcon;

        [Signal]
        public delegate void TextEndEventHandler();
        public override void _Ready()
        {
            _options = (VBoxContainer?)NodeFinder.FindBFSCached(this, "Options");
            _text = (RichTextLabel?)NodeFinder.FindBFSCached(this, "Text");
            _playerIcon = (TextureRect?)NodeFinder.FindBFSCached(this, "Avatar");
            EmitSignal(SignalName.TextEnd);
        }

        public void SetAvatar(Texture2D icon) => _playerIcon!.Texture = icon;
        public void SetOptions(Label option) => _options?.AddChild(option);
        public async void UpdateText(string text)
        {
            _text!.Text = "";
            foreach (char c in text)
            {
                _text.Text += c;
                await ToSignal(GetTree().CreateTimer(0.015f), "timeout");
            }
            await ToSignal(GetTree().CreateTimer(1f), "timeout");
            EmitSignal(SignalName.TextEnd);
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
    }
}
