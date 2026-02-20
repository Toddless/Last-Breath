namespace LastBreath.Script.UI
{
    using Godot;
    using Core.Data;
    using Core.Interfaces.UI;
    using Core.Interfaces.Events;
    using Core.Interfaces.MessageBus;

    public partial class PlayerHUD : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://boqqyrt0sfpve";

        [Export] private Button? _characterBtn, _inventoryBtn, _questsBtn, _craftingBtn;
        [Export] private TextureProgressBar? _playerHealth;
        [Export] private GridContainer? _playerEffects;

        private IGameMessageBus? _gameMessageBus;

        public override void _Ready()
        {
            _characterBtn.Pressed += OnCharacterBtnPressed;
            _inventoryBtn.Pressed += OnIntenoryBtnPressed;
            _questsBtn.Pressed += OnQuestBtnPressed;
            _craftingBtn.Pressed += OnCraftingBtnPressed;
        }

        private void OnCraftingBtnPressed() => _gameMessageBus?.PublishAsync(new OpenCraftingWindowEvent(string.Empty));
        private void OnQuestBtnPressed() => _gameMessageBus?.PublishAsync(new OpenQuestWindowEvent());
        private void OnIntenoryBtnPressed() => _gameMessageBus?.PublishAsync(new OpenInventoryWindowEvent());
        private void OnCharacterBtnPressed() => _gameMessageBus?.PublishAsync(new OpenCharacterWindowEvent());

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameMessageBus = provider.GetService<IGameMessageBus>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
