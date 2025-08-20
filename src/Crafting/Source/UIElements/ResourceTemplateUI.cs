namespace Crafting.Source.UIElements
{
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class ResourceTemplateUI : MarginContainer
    {
        [Export] private Label? _text;
        [Export] private TextureRect? _icon;

        public void SetText(string displayName, int amountHave, int amounNeed)
        {
            if (_text == null) return;
            var settings = new LabelSettings();
            if (amountHave >= amounNeed) settings.FontColor = new Color(Colors.Green);
            else settings.FontColor = new Color(Colors.Red);
            _text.LabelSettings = settings;
            _text.Text = $"{displayName} : {amountHave} / {amounNeed}";
        }

        public void SetIcon(Texture2D icon) => _icon.Texture = icon;
        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>("res://Source/UIElements/View/ResourceTemplateUI.tscn");
    }
}
