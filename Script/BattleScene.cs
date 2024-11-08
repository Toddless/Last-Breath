namespace Playground
{
    using Godot;
    using Playground.Script;
    using Playground.Script.Helpers;

    public partial class BattleScene : Node2D
    {
        private EnemyAI? _enemy;
        private Player? _player;
        private Sprite2D? _sprite;
        private GlobalSignals? _signals;
        private ProgressBar? _playerHpBar;
        private ProgressBar? _enemyHpBar;
        private Button? _damageButton;
        private Button? _returnButton;
        private Timer? _timer;
        private EnemyInventory? _enemyInventory;

        [Signal]
        public delegate void BattleSceneReadyEventHandler();
        [Signal]
        public delegate void BattleSceneFinishedEventHandler();
        [Signal]
        public delegate void EnemyTurnEventHandler();
        [Signal]
        public delegate void PlayerTurnEventHandler();


        public override void _Ready()
        {
            StartFight();
            var battelScene = GetParent().GetNode<BattleScene>("BattleScene");
            _signals = battelScene.GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _playerHpBar = battelScene.GetNode<ProgressBar>("UI/PlayerHpBar");
            _enemyHpBar = battelScene.GetNode<ProgressBar>("UI/EnemyHpBar");
            _sprite = battelScene.GetNode<Sprite2D>("BattleScene1");
            _damageButton = battelScene.GetNode<Button>("UI/Button");
            _returnButton = battelScene.GetNode<Button>("UI/ReturnButton");
            _timer = battelScene.GetNode<Timer>("Timer");
            _enemyInventory = GetNode<EnemyInventory>(NodePathHelper.EnemyInventory);
            _returnButton.Pressed += BattleFinished;
            _player!.PlayerHealth!.OnCharacterDied += PlayerHealth_OnCharacterDied;
            _enemyInventory!.TakeAllButton!.Pressed += PlayerTakedAllItems;
            _enemyInventory!.CloseButton!.Pressed += PlayerClosedInventory;
            _enemy!.Health!.OnCharacterDied += EnemyDied;
            this.EnemyTurn += EnemyDealDamage;
            this.PlayerTurn += PlayersTurn;
            _damageButton.Pressed += PlayersDealDamage;
            SetHealthBars();
            UpdateHealthBar();
        }
        private void PlayersDealDamage()
        {
            if (_enemy!.Health!.CurrentHealth <= 0)
            {
                return;
            }
            var x = _player!.PlayerAttack!.CalculateDamage();
            _enemy!.Health!.TakeDamage(x);
            UpdateHealthBar();
            _damageButton!.Visible = false;
            EmitSignal(SignalName.EnemyTurn);
        }

        public void EnemyDealDamage()
        {
            if (_enemy!.Health!.CurrentHealth > 0)
            {
                var x = _enemy!.EnemyDealDamage();
                _player!.PlayerHealth!.TakeDamage(x);
                UpdateHealthBar();
                GD.Print($"Enemy did damage: {Mathf.RoundToInt(x)}");
                GD.Print("___________________________");

                EmitSignal(SignalName.PlayerTurn);
            }
        }

        private void PlayerClosedInventory()
        {
            _enemyInventory!.ClearInventory();
            BattleFinished();
        }

        private void PlayerTakedAllItems()
        {
            _enemyInventory!.GivePlayerItems().ForEach(_player!.Inventory!.AddItem);
            _enemyInventory.ClearInventory();
            BattleFinished();
        }

        private void BattleFinished()
        {
            SetPlayerStats();
            _enemy!.QueueFree();
            QueueFree();
            EmitSignal(SignalName.BattleSceneFinished);
        }

        private void SetPlayerStats()
        {
            _player!.Position = _player.PlayerLastPosition;
            _player.CanMove = true;
        }

        private void EnemyDied()
        {
            _damageButton!.Visible = false;
            _enemyInventory!.InventoryVisible(true);

        }

        private void PlayerHealth_OnCharacterDied()
        {
            GD.Print("GameOver");
            QueueFree();
        }

        public void Init(Player player, EnemyAI enemy)
        {
            _player = player;
            _enemy = enemy;
            _player.CanMove = false;

            _player.Position = new Vector2(250, 450);
            _enemy.Position = new Vector2(950, 450);
        }

        private void SetHealthBars()
        {
            _playerHpBar!.MaxValue = _player!.PlayerHealth!.MaxHealth;
            _enemyHpBar!.MaxValue = _enemy!.Health!.MaxHealth;
        }

        public void UpdateHealthBar()
        {
            _enemyHpBar!.Value = _enemy!.Health!.CurrentHealth;
            _playerHpBar!.Value = _player!.PlayerHealth!.CurrentHealth;
        }

        private void PlayersTurn()
        {
            _damageButton!.Visible = true;
        }

        public void StartFight()
        {
            if (_player == null && _enemy == null)
            {
                GD.Print("Opponents not found");
            }
        }
    }
}
