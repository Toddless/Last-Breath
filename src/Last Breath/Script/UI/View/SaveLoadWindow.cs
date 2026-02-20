namespace LastBreath.Script.UI
{
    using Core.Data;
    using Core.Interfaces.MessageBus;
    using Godot;
    using Core.Interfaces.UI;

    public partial class SaveLoadWindow : Window, IInitializable, IRequireServices
    {
        private const string UID = "uid://cserxppd6wiui";
        [Export] private Button? _returnButton, _loadBtn, _deleteBtn;

        private IGameMessageBus? _gameMessageBus;

        public override void _Ready()
        {
            _returnButton.Pressed += RaiseClose;

        }

        public override void InjectServices(IGameServiceProvider provider)
        {
            _gameMessageBus = provider.GetService<IGameMessageBus>();
        }



        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
