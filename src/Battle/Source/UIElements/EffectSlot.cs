namespace Battle.Source.UIElements
{
    using Core.Interfaces.UI;
    using Core.Views;
    using Godot;

    public partial class EffectSlot : Control, IInitializable
    {
        private const string UID = "uid://5n5bfrh72v8s";
        private string _description = string.Empty;
        private int _duration;
        [Export] private TextureRect? _effectIcon;
        [Export] private Label? _effectStacks;

        public string EffectId { get; private set; } = string.Empty;

        public override GodotObject _MakeCustomTooltip(string forText)
        {
            var richText = new RichTextLabel { Text = $"{_description}\n Duration: {_duration}", AutowrapMode = TextServer.AutowrapMode.Off, FitContent = true, BbcodeEnabled = true };
            return richText;
        }

        /// <summary>Binds the aggregated view: icon, stack count (hidden when 1) and remaining duration.</summary>
        public void SetView(EffectView view)
        {
            EffectId = view.Id;
            _description = view.Description;
            _duration = view.Duration;
            _effectIcon?.Texture = view.Icon;
            _effectStacks?.Text = view.Stacks <= 1 ? string.Empty : view.Stacks.ToString();
        }

        public void RemoveEffect()
        {
            EffectId = string.Empty;
            QueueFree();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
