namespace Playground.Script.UI.View
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class DialogWindow : Control
    {
        private VBoxContainer? _options;
        private RichTextLabel? _text;
        private TextureRect? _playerIcon;

        public override void _Ready()
        {
            _options = (VBoxContainer?)NodeFinder.FindBFSCached(this, "Options");
            _text = (RichTextLabel?)NodeFinder.FindBFSCached(this, "Text");
            _playerIcon = (TextureRect?)NodeFinder.FindBFSCached(this, "Avatar");
        }

        public void SetAvatar(Texture2D icon) => _playerIcon!.Texture = icon;
        public void SetOptions(Label option) => _options?.AddChild(option);
        public void UpdateText(string text) => _text!.Text = text;

    }
}
