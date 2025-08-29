namespace Crafting.Source.UIElements
{
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class ItemCreatedNotifier : Control
    {
        private const string UID = "uid://cu1u7ht1lp0hc";
        [Export] private TextureRect? _itemImage;
        [Export] private VBoxContainer? _statContainer;
        [Export] private Button? _okButton;

        [Signal] public delegate void OkButtonPressedEventHandler();

        public override void _Ready()
        {
            if (_okButton != null) _okButton.Pressed += OnButtonPressed;
        }

        private void OnButtonPressed()
        {
            EmitSignal(SignalName.OkButtonPressed);
            this.QueueFree();
        }

        public void SetImage(Texture2D icon)
        {
            if(_itemImage != null) _itemImage.Texture = icon;
        }

        public void SetText(Label label) => _statContainer?.AddChild(label);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
