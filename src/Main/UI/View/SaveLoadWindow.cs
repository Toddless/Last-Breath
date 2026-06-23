namespace LastBreath.UI.View
{
    using Core.Data;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.UI;
    using Godot;

    public partial class SaveLoadWindow : Control, IWindow
    {
        private const string UID = "uid://cserxppd6wiui";
        [Export] private Button? _returnButton, _loadBtn, _deleteBtn;

        private IGameMessageBus? _gameMessageBus;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            _returnButton?.Pressed += Close;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameMessageBus = provider.GetService<IGameMessageBus>();
        }

        public void Close() => GetParent().RemoveChild(this);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
