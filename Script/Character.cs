namespace Playground.Script
{
    using Godot;
    using Godot.Collections;

    public partial class Character : Node
    {
        [Export]
        private bool _isPlayer;
        [Export]
        private float _currentHealth;
        [Export]
        private float _maxHealth = 100;
        [Export]
        private Array<CombatAction> _combatActions;
        [Export]
        private Node _opponent;
        [Export]
        private Texture2D _visual;
        [Export]
        private bool _flipVisual;
        private Sprite2D _sprite;

        private Label _healthBarText;
        private ProgressBar _healthBar;

        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>("Sprite2D");
            _healthBar = GetNode<ProgressBar>("HealthBar");
            _healthBarText = GetNode<Label>("HealthBar/HealthBarText");
            _sprite.Texture = _visual;
            _sprite.FlipH = _flipVisual;
            _currentHealth = _maxHealth;
            _healthBar.MaxValue = _maxHealth;
            // получаем корневую ноду
            var mainNode = GetNode("/root");
            // получаем скрипт менеджера боя
            var battleManager = mainNode.GetNode<BattleTurnManager>("BattleScene");
            // подписываемся на событие начала хода
            battleManager.OnCharacterBeginTurn += OnCharacterBegidTurn;
        }

        public void CastCombatAction(CombatAction action)
        {

        }

        private void UpdateHealthBar()
        {
            // обновляем значение healthBar
            _healthBar.Value = _currentHealth;
            // обновляем текст
            _healthBarText.Text = _currentHealth.ToString();
        }

        private void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            // если здоровье меньше 0
            if (_currentHealth < 0)
            {
                GetNode<BattleTurnManager>("BattleTurnManager").EmitSignal("OnCharacterDied", this);
                // вызываем метод GameOver
                QueueFree();
            }
            UpdateHealthBar();
        }

        private void Heal(float amount)
        {
            _currentHealth += amount;
            if (_currentHealth > _maxHealth)
                // если здоровье больше максимального, присваиваем максимальное значение
                _currentHealth = _maxHealth;
            UpdateHealthBar();
        }

        private void OnCharacterBegidTurn(Node character)
        {

        }
    }
}
