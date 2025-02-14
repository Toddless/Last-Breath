namespace Playground.Script.UI
{
    using System;
    using Godot;

    public partial class MainUI : Control
    {
        private Button? _characterBtn, _inventoryBtn, _questsBtn, _mapBtn, _debugBtn;
        private TextureProgressBar? _playerHealth;
        private GridContainer? _playerEffects;

        public event Action? Character, Inventory, Quests, Map, Debug;
        public event Action? PlayerHealthChanged, PlayerEffectsChanged;

        public override void _Ready()
        {
            var root = GetNode<MarginContainer>(nameof(MarginContainer));
            var buttons = root.GetNode<HBoxContainer>("HBoxContainerButtons");
            _playerHealth = root.GetNode<VBoxContainer>(nameof(VBoxContainer)).GetNode<TextureProgressBar>("Health");
            _playerEffects = root.GetNode<VBoxContainer>(nameof(VBoxContainer)).GetNode<GridContainer>(nameof(GridContainer));
            _inventoryBtn = buttons.GetNode<Button>("Inventory");
            _questsBtn = buttons.GetNode<Button>("Quests");
            _characterBtn = buttons.GetNode<Button>("Character");
            _mapBtn = buttons.GetNode<Button>("Map");
            var player = GameManager.Instance.Player;
            if (player != null)
            {
                _playerHealth.MaxValue = player.HealthComponent!.MaxHealth;
                _playerHealth.Value = player.HealthComponent.CurrentHealth;
            }

            SetEvents();
        }

        public void UpdatePlayerHealthBar(int value) => _playerHealth!.Value = value;
        public void UpdateMaxHealthBar(int value) => _playerHealth!.MaxValue = value;

        private void SetEvents()
        {
            _inventoryBtn!.Pressed += () => Inventory?.Invoke();
            _characterBtn!.Pressed += () => Character?.Invoke();
            _questsBtn!.Pressed += () => Quests?.Invoke();
            _mapBtn!.Pressed += () => Map?.Invoke();
            //_debugBtn.Pressed += () => Debug?.Invoke();
        }

    }
}
