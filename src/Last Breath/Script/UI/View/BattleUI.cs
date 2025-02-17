namespace Playground
{
    using System;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.ScenesHandlers;

    public partial class BattleUI : Control
    {
        private Button? _returnButton;
        private TextureProgressBar? _playerHealthBar, _enemyHealthBar;
        private GridContainer? _playerEffects, _enemyEffects;
        private RandomNumberGenerator? _rnd;
        private TextureButton? _dexterityStance, _strengthStance, _intelligenceStance, _head, _body, _legs;
        private BattleSceneHandler? _battleSceneHandler;
        private Panel? _panelPlayer, _panelEnemy;
        // ability buttons left

        public Action? Return;

        public override void _Ready()
        {
            _dexterityStance = (TextureButton?)NodeFinder.FindBFSCached(this, "Dexterity");
            _strengthStance = (TextureButton?)NodeFinder.FindBFSCached(this, "Intelligence");
            _intelligenceStance = (TextureButton?)NodeFinder.FindBFSCached(this, "Strength");

            _head = (TextureButton?)NodeFinder.FindBFSCached(this, "Head");
            _body = (TextureButton?)NodeFinder.FindBFSCached(this, "Body");
            _legs = (TextureButton?)NodeFinder.FindBFSCached(this, "Legs");

            _playerHealthBar = (TextureProgressBar?)NodeFinder.FindBFSCached(this, "PlayerHealth");
            _enemyHealthBar = (TextureProgressBar?)NodeFinder.FindBFSCached(this, "EnemyHealth");
            _playerEffects = (GridContainer?)NodeFinder.FindBFSCached(this, "PlayerEffects");
            _enemyEffects = (GridContainer?)NodeFinder.FindBFSCached(this, "EnemyEffects");
            _returnButton = (Button?)NodeFinder.FindBFSCached(this, "ReturnButton");

            _panelPlayer = (Panel?)NodeFinder.FindBFSCached(this, "Panel");
            _panelEnemy = (Panel?)NodeFinder.FindBFSCached(this, "Panel2");
            _returnButton!.Pressed += () => Return?.Invoke();
            NodeFinder.ClearCache();
        }

        public void InitialSetup(Player player, BaseEnemy enemy)
        {
            _playerHealthBar.MaxValue = player.HealthComponent.MaxHealth;
            _playerHealthBar.Value = player.HealthComponent.CurrentHealth;

            _enemyHealthBar.MaxValue = enemy.HealthComponent.MaxHealth;
            _enemyHealthBar.Value = enemy.HealthComponent.CurrentHealth;
            player.Position = _panelPlayer.GlobalPosition;
            enemy.Position = _panelEnemy.GlobalPosition;
        }

        public void OnPlayerCurrentHealthChanged(float newValue) => _playerHealthBar!.Value = newValue;

        public void OnEnemyCurrentHealthChanged(float newValue) => _enemyHealthBar!.Value = newValue;

        public void OnPlayerMaxHealthChanged(float newValue) => _playerHealthBar!.MaxValue = newValue;

        public void OnEnemyMaxHealthChanged(float newValue) => _enemyHealthBar!.MaxValue = newValue;
    }
}
