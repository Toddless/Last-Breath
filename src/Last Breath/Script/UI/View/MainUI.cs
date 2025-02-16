namespace Playground.Script.UI
{
    using System;
    using Godot;
    using Playground.Script.Helpers;

    public partial class MainUI : Control
    {
        private Button? _characterBtn, _inventoryBtn, _questsBtn, _mapBtn;
        private TextureProgressBar? _playerHealth;
        private GridContainer? _playerEffects;

        public event Action? Character, Inventory, Quests, Map, Debug;
        public event Action? PlayerHealthChanged, PlayerEffectsChanged;

        public override void _Ready()
        {
            _playerHealth = (TextureProgressBar?)NodeFinder.FindBFSCached(this, "Health");
            _playerEffects = (GridContainer?)NodeFinder.FindBFSCached(this, "Effects");
            _inventoryBtn = (Button?)NodeFinder.FindBFSCached(this, "Inventory");
            _questsBtn = (Button?)NodeFinder.FindBFSCached(this, "Quests");
            _characterBtn = (Button?)NodeFinder.FindBFSCached(this, "Character");
            _mapBtn = (Button?)NodeFinder.FindBFSCached(this, "Map");
            var player = GameManager.Instance.Player;
            if (player != null)
            {
                _playerHealth!.MaxValue = player.HealthComponent!.MaxHealth;
                _playerHealth.Value = player.HealthComponent.CurrentHealth;
            }

            NodeFinder.ClearCache();
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
        }
    }
}
