namespace Playground
{
    using Godot;

    public partial class BattleUI : Control
    {
        private Button? _returnButton;
        private TextureProgressBar? _playerHealthBar, _enemyHealthBar;
        private GridContainer? _playerEffects, _enemyEffects;
        private RandomNumberGenerator? _rnd;
        private TextureButton? _dexterityStance, _strengthStance, _intelligenceStance, _head, _body, _legs;
        private BaseEnemy? _enemy;
        private Player? _player;
        // ability buttons left

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
        }

        private void SetHealthBars()
        {
        }

        private void UpdateHealthBar()
        {
        }
    }
}
