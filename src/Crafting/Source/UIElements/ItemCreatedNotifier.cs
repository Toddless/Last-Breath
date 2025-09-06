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
        [Export] private Button? _okButton, _destroyButton;

        [Signal] public delegate void OkButtonPressedEventHandler();
        [Signal] public delegate void DestroyButtonPressedEventHandler();

        public override void _Ready()
        {
            if (_okButton != null) _okButton.Pressed += () =>
            {
                EmitSignal(SignalName.OkButtonPressed);
                this.QueueFree();
            };
            if (_destroyButton != null) _destroyButton.Pressed += () =>
            {
                EmitSignal(SignalName.DestroyButtonPressed);
                this.QueueFree();
            };
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_accept"))
            {
                EmitSignal(SignalName.OkButtonPressed);
                GetViewport().SetInputAsHandled();
                QueueFree();
            }
        }

        public void SetImage(Texture2D? icon)
        {
            if (_itemImage != null && icon != null) _itemImage.Texture = icon;
        }

        public void SetText(Label label) => _statContainer?.AddChild(label);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
