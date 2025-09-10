namespace Crafting.Source.UIElements
{
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class ResourceTemplateUI : MarginContainer
    {
        private const string UID = "res://Source/UIElements/View/ResourceTemplateUI.tscn";
        [Export] private Label? _text;
        [Export] private TextureRect? _icon;

        public void SetText(string displayName, int have, int need)
        {
            if (_text != null)
            {
                _text.LabelSettings = new();
                if (have >= need) _text!.LabelSettings.FontColor = new Color(Colors.Green);
                else _text!.LabelSettings.FontColor = new Color(Colors.Red);
                _text.Text = $"{displayName} : {have} / {need}";
            }
        }

        public void SetIcon(Texture2D? icon)
        {
            if(_icon != null) _icon.Texture = icon;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
