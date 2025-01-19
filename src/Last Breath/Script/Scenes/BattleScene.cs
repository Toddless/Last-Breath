namespace Playground
{
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script;
    using Playground.Script.Helpers;

    [Inject]
    public partial class BattleScene : Node2D
    {
        private Button? _intelligenceStance, _dexterityStance, _strengthStance;
        private ProgressBar? _playerHpBar, _enemyHpBar;
        private Button? _head, _body, _legs;
        private RandomNumberGenerator? _rnd;
        private GlobalSignals? _signals;
        private Button? _returnButton, _takeAll;
        private Sprite2D? _sprite;
        private EnemyAI? _enemy;
        private Player? _player;
        private Timer? _timer;
        private Node2D? _attackButtonsUI;
        private IInventory? _playerInventory;
        private IInventory? _enemyInventory;

        [Signal]
        public delegate void BattleSceneFinishedEventHandler(EnemyAI enemy);
        [Signal]
        public delegate void EnemyTurnEventHandler();
        [Signal]
        public delegate void PlayerTurnEventHandler();

        [Inject]
        protected RandomNumberGenerator? Rnd
        {
            get => _rnd;
            set => _rnd = value;
        }

        public override void _Ready()
        {
            StartFight();
            var battelScene = GetParent().GetNode<BattleScene>("BattleScene");
            var battleSceneUi = battelScene.GetNode("UI");
            _attackButtonsUI = battleSceneUi.GetNode<Node2D>("AttackButtons");
            var stanceButtons = _attackButtonsUI.GetNode("Stance");
            var bodyPartButtons = _attackButtonsUI.GetNode("BodyPart");

            _takeAll = battleSceneUi.GetNode<Button>("TakeAllButton");
            _dexterityStance = stanceButtons.GetNode<Button>("DexterityStance");
            _strengthStance = stanceButtons.GetNode<Button>("StrengthStance");
            _intelligenceStance = stanceButtons.GetNode<Button>("IntelligenceStance");
            _head = bodyPartButtons.GetNode<Button>("Head");
            _body = bodyPartButtons.GetNode<Button>("Body");
            _legs = bodyPartButtons.GetNode<Button>("Legs");
            _signals = battelScene.GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _playerHpBar = battleSceneUi.GetNode<ProgressBar>("PlayerHpBar");
            _enemyHpBar = battleSceneUi.GetNode<ProgressBar>("EnemyHpBar");
            _sprite = battelScene.GetNode<Sprite2D>("BattleScene1");
            _returnButton = battleSceneUi.GetNode<Button>("ReturnButton");

            _takeAll.Pressed += PlayerTakedAllItems;
            _timer = battelScene.GetNode<Timer>("Timer");
            _returnButton.Pressed += BattleFinished;
            PlayerTurn += PlayerMakeTurn;
            EnemyTurn += EnemyMakeTurn;
            _head.Pressed += PlayerMakeTurn;
            SetHealthBars();
            UpdateHealthBar();
        }

        public void ResolveDependencies() => DiContainer.InjectDependencies(this);

        public void Init(Player player, EnemyAI enemy)
        {
            _player = player;
            _enemy = enemy;
            _player.CanMove = false;
            _enemy.EnemyFigth = true;
            _player.Position = new Vector2(250, 450);
            _enemy.Position = new Vector2(950, 450);
            _enemyInventory = _enemy.Inventory;
            _playerInventory = _player.Inventory;
        }

        private void StartFight()
        {
            if (_enemy == null)
            {
                SetPlayerStats();
                this.CallDeferred("remove_child", _player!);
                GetParent().CallDeferred("add_child", _player!);
                this.QueueFree();
            }
        }

        private void EnemyMakeTurn()
        {
            if (_enemy!.EnemyHealth!.CurrentHealth > 0)
            {
                EnemyTurnHandler();
                EmitSignal(SignalName.PlayerTurn);
            }
            else
            {
                EnemyDead();
            }
        }

        private void PlayerMakeTurn()
        {
            if (_enemy!.EnemyHealth!.CurrentHealth <= 0)
            {
                EnemyDead();
                return;
            }
            _attackButtonsUI?.Show();
            var (damage, crit) = _player!.PlayerAttack!.CalculateDamage();
            // TODO: Player attack animation
            _enemy!.EnemyHealth!.CurrentHealth -= damage;

            if (crit)
            {
                // TODO: Crit animation or some nice text 
                GD.Print("Crit!");
            }
            UpdateHealthBar();
            //_damageButton!.Visible = false;
            _attackButtonsUI?.Hide();
            EmitSignal(SignalName.EnemyTurn);
        }


        private void EnemyTurnHandler()
        {
            _enemy!.BattleBehavior?.GatherInfo(_player!);
            float additionalAttackChance = Rnd!.RandfRange(0, 1);
            var (damage, crit) = _enemy.ActivateAbilityBeforDealDamage();

            // TODO: Ability animation

            _player!.PlayerHealth!.CurrentHealth -= damage;
            if (crit)
            {
                // TODO: Crit animation or some nice text 
                GD.Print("Crit!");
            }
            if (_enemy.BattleBehavior!.AbilityWithEffectAfterAttack != null)
            {
                // TODO: Animation
                UpdateHealthBar();
            }
            UpdateHealthBar();
            _enemy.BattleBehavior.RemoveBuffEffectAfterTurnsEnd();

            GD.Print($"dealed damage {damage}");
            if (additionalAttackChance <= _enemy.EnemyAttack!.BaseAdditionalAttackChance)
            {
                EnemyTurnHandler();
            }
        }

        private void PlayerClosedInventory()
        {
            BattleFinished();
        }

        private void PlayerTakedAllItems()
        {
            var enemyItems = _enemyInventory?.GiveAllItems();
            if (enemyItems?.Count > 0)
                _playerInventory?.TakeAllItems(enemyItems);
            BattleFinished();
        }

        private void BattleFinished()
        {
            SetPlayerStats();
            this.CallDeferred("remove_child", _player!);
            GetParent().CallDeferred("add_child", _player!);
            EmitSignal(SignalName.BattleSceneFinished, _enemy!);
        }

        private void SetPlayerStats()
        {
            _player!.Position = _player.PlayerLastPosition;
            _player.CanMove = true;
        }

        private void EnemyDead()
        {
            _attackButtonsUI?.Hide();
            _enemyInventory?.ShowInventory?.Invoke();
            _enemy?.BattleBehavior?.BattleEnds();
        }

        private void SetHealthBars()
        {
            _playerHpBar!.MaxValue = _player!.PlayerHealth!.MaxHealth;
            _enemyHpBar!.MaxValue = _enemy!.EnemyHealth!.MaxHealth;
        }

        private void UpdateHealthBar()
        {
            _enemyHpBar!.Value = _enemy!.EnemyHealth!.CurrentHealth;
            _playerHpBar!.Value = _player!.PlayerHealth!.CurrentHealth;
        }
    }
}
