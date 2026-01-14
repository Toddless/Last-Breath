namespace LastBreath.Script.UI
{
    using Godot;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;

    public partial class PlayerHUD : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://boqqyrt0sfpve";

        [Export] private LocalizableButton? _characterBtn, _inventoryBtn, _questsBtn, _craftingBtn;
        [Export] private TextureProgressBar? _playerHealth;
        [Export] private GridContainer? _playerEffects;

        private IUiMediator? _uiMediator;

        public override void _Ready()
        {
            _characterBtn.Pressed += OnCharacterBtnPressed;
            _inventoryBtn.Pressed += OnIntenoryBtnPressed;
            _questsBtn.Pressed += OnQuestBtnPressed;
            _craftingBtn.Pressed += OnCraftingBtnPressed;
        }

        public override void _EnterTree()
        {
            if (_uiMediator != null) _uiMediator.UpdateUi += UpdateUI;
        }

        public override void _ExitTree()
        {
            if (_uiMediator != null) _uiMediator.UpdateUi -= UpdateUI;
        }

        private void OnCraftingBtnPressed() => _uiMediator?.Publish(new OpenCraftingWindowEvent(string.Empty));
        private void OnQuestBtnPressed() => _uiMediator?.Publish(new OpenQuestWindowEvent());
        private void OnIntenoryBtnPressed() => _uiMediator?.Publish(new OpenInventoryWindowEvent());
        private void OnCharacterBtnPressed() => _uiMediator?.Publish(new OpenCharacterWindowEvent());

        public void InjectServices(IGameServiceProvider provider)
        {
            _uiMediator = provider.GetService<IUiMediator>();
        }

        private void UpdateUI()
        {
            _characterBtn?.UpdateButtonText();
            _inventoryBtn?.UpdateButtonText();
            _questsBtn?.UpdateButtonText();
            _craftingBtn?.UpdateButtonText();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
