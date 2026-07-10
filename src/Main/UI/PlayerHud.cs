namespace LastBreath.UI
{
    using Core.Data;
    using Core.Events;
    using Core.MessageBus;
    using Core.Views.UI;
    using Crafting.Source.UIElements;
    using Godot;
    using LastBreath.Inventory;

    public partial class PlayerHud : Control, IHud
    {
        private const string UID = "uid://boqqyrt0sfpve";

        [Export] private Button? _characterBtn, _inventoryBtn, _questsBtn, _craftingBtn;
        [Export] private TextureProgressBar? _playerHealth;
        [Export] private GridContainer? _playerEffects;

        private IGameMessageBus? _gameMessageBus;

        public override void _Ready()
        {
            _characterBtn?.Pressed += OnCharacterBtnPressed;
            _inventoryBtn?.Pressed += OnInventoryBtnPressed;
            _questsBtn?.Pressed += OnQuestBtnPressed;
            _craftingBtn?.Pressed += OnCraftingBtnPressed;
        }

        public void Remove() => GetParent().RemoveChild(this);

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameMessageBus = provider.GetService<IGameMessageBus>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        // TODO:
        // старый подход. Инжектим uiElementsManager и открываем окна с его помощью
        private void OnCraftingBtnPressed() => _gameMessageBus?.PublishMessageAsync(new OpenWindowMessage(typeof(CraftingWindow)));
        private void OnQuestBtnPressed() => _gameMessageBus?.PublishMessageAsync(new OpenWindowMessage(typeof(QuestJournalWindow)));
        private void OnInventoryBtnPressed() => _gameMessageBus?.PublishMessageAsync(new OpenWindowMessage(typeof(InventoryWindow)));
        private void OnCharacterBtnPressed() => _gameMessageBus?.PublishMessageAsync(new OpenWindowMessage(typeof(CharacterWindow)));

    }
}
