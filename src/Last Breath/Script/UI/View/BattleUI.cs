namespace Playground
{
    using Godot;
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

        public Button? ReturnButton
        {
            get => _returnButton;
        }

        public override void _Ready()
        {
            var root = GetNode<MarginContainer>(nameof(MarginContainer));
            var abilityContainer = root.GetNode<HBoxContainer>("HBoxContainerAbilityButtons");
            var vBoxContainer = root.GetNode<VBoxContainer>(nameof(VBoxContainer));
            var statusEffects = vBoxContainer.GetNode<HBoxContainer>("HBoxContainerStatusEffects");
            var healthBars = vBoxContainer.GetNode<HBoxContainer>("HBoxContainerHealthBars");
            var opponents = root.GetNode<VBoxContainer>("VBoxContainerOpponents").GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<HBoxContainer>(nameof(HBoxContainer));
            var stanceButtons = root.GetNode<HBoxContainer>("HBoxContainerAttackButtons").GetNode<VBoxContainer>("VBoxContainerStance");
            var bodyParts = root.GetNode<HBoxContainer>("HBoxContainerAttackButtons").GetNode<VBoxContainer>("VBoxContainerBodyPart");

            _dexterityStance = stanceButtons.GetNode<TextureButton>("DexterityStance");
            _strengthStance = stanceButtons.GetNode<TextureButton>("IntelligenceStance");
            _intelligenceStance = stanceButtons.GetNode<TextureButton>("StrengthStance");

            _head = bodyParts.GetNode<TextureButton>("Head");
            _body = bodyParts.GetNode<TextureButton>("Body");
            _legs = bodyParts.GetNode<TextureButton>("Legs");

            _playerHealthBar = healthBars.GetNode<TextureProgressBar>("TextureProgressBarPlayer");
            _enemyHealthBar = healthBars.GetNode<TextureProgressBar>("TextureProgressBarEnemy");
            _playerEffects = statusEffects.GetNode<GridContainer>("PlayerEffects");
            _enemyEffects = statusEffects.GetNode<GridContainer>("EnemyEffects");
            _returnButton = vBoxContainer.GetNode<Button>(nameof(Button));

            _panelPlayer = opponents.GetNode<Panel>(nameof(Panel));
            _panelEnemy = opponents.GetNode<Panel>("Panel2");
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
