namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Core.Interfaces.UI;

    [Tool]
    [GlobalClass]
    public partial class ClickableResource : ClickableItem, IInitializable
    {
        private const string UID = "uid://naq3akxs2d3o";
        private Action? _onRightClick;
        private Variant _meta;
        [Export] private Label? _text, _quantity;
        [Export] private TextureRect? _icon;

        public override void _Ready()
        {
            RightClick += () => _onRightClick?.Invoke();
        }

        public void SetText(string displayName, int have, int need)
        {
            if (_text != null && _quantity != null)
            {
                _text.Text = $"{displayName}";
                _text.HorizontalAlignment = HorizontalAlignment.Center;

                _quantity.LabelSettings = new();
                if (have >= need) _quantity.LabelSettings.FontColor = new Color(Colors.Green);
                else _quantity.LabelSettings.FontColor = new Color(Colors.Red);
                _quantity.Text = $"{have} / {need}";
                _quantity.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        public void SetIcon(Texture2D? icon)
        {
            if (_icon != null)_icon.Texture = icon;
        }

        public void SetMetadata(Variant metadata) => _meta = metadata;

        public void SetRightClickAction(Action? rightClickAction) => _onRightClick = rightClickAction;

        public Variant GetMetadata() => _meta;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        protected override void OnMouseEntered()
        {

        }

        protected override void OnMouseExited()
        {

        }
    }
}
