namespace Playground
{
    	using Godot;
    	using Playground.Script;
    	using Playground.Script.Helpers;

    	public partial class BattleScene : Node2D
    	{
    		private EnemyInventory? _enemyInventory;
    		private RandomNumberGenerator? _rnd;
    		private ProgressBar? _playerHpBar;
    		private ProgressBar? _enemyHpBar;
    		private GlobalSignals? _signals;
    		private Button? _damageButton;
    		private Button? _returnButton;
    		private Sprite2D? _sprite;
    		private EnemyAI? _enemy;
    		private Player? _player;
    		private Timer? _timer;

    		[Signal]
    		public delegate void BattleSceneFinishedEventHandler(EnemyAI enemy);
    		[Signal]
    		public delegate void EnemyTurnEventHandler();
    		[Signal]
    		public delegate void PlayerTurnEventHandler();


    		public override void _Ready()
    		{
    			StartFight();
    			var battelScene = GetParent().GetNode<BattleScene>("BattleScene");
    			var battleSceneUi = battelScene.GetNode("UI");
    			_signals = battelScene.GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
    			_playerHpBar = battleSceneUi.GetNode<ProgressBar>("PlayerHpBar");
    			_enemyHpBar = battleSceneUi.GetNode<ProgressBar>("EnemyHpBar");
    			_sprite = battelScene.GetNode<Sprite2D>("BattleScene1");
    			_damageButton = battleSceneUi.GetNode<Button>("Button");
    			_returnButton = battleSceneUi.GetNode<Button>("ReturnButton");
    			_rnd = new RandomNumberGenerator();
    			_timer = battelScene.GetNode<Timer>("Timer");
    			_enemyInventory = GetNode<EnemyInventory>(NodePathHelper.EnemyInventory);
    			_enemyInventory.InventoryVisible(false);
    			_returnButton.Pressed += BattleFinished;
    			_enemyInventory!.TakeAllButton!.Pressed += PlayerTakedAllItems;
    			_enemyInventory!.CloseButton!.Pressed += PlayerClosedInventory;
    			_damageButton.Pressed += PlayerMakeTurn;
    			PlayerTurn += PlayersTurn;
    			EnemyTurn += EnemyMakeTurn;
    			SetHealthBars();
    			UpdateHealthBar();
    		}

    		public void Init(Player player, EnemyAI enemy)
    		{
    			_player = player;
    			_enemy = enemy;
    			_player.CanMove = false;
    			_enemy.EnemyFigth = true;
    			_player.Position = new Vector2(250, 450);
    			_enemy.Position = new Vector2(950, 450);
    		}

    		private void StartFight()
    		{
    			if (_enemy == null)
    			{
    				GD.Print("Opponents not found");
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
    			var (damage, crit) = _player!.PlayerAttack!.CalculateDamage();
    			// TODO: Player attack animation
    			_enemy!.EnemyHealth!.CurrentHealth -= damage;

    			if (crit)
    			{
    				// TODO: Crit animation or some nice text 
    				GD.Print("Crit!");
    			}
    			UpdateHealthBar();
    			_damageButton!.Visible = false;
    			EmitSignal(SignalName.EnemyTurn);
    		}


    		private void EnemyTurnHandler()
    		{
    			_rnd ??= new();
    			_enemy!.BattleBehavior?.GatherInfo(_player!);
    			float additionalAttackChance = _rnd.RandfRange(0, 1);
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
    			_enemyInventory!.OnDeathSpawnItem();
    			_enemy!.BattleBehavior!.BattleEnds();
    			_damageButton!.Visible = false;
    			_enemyInventory!.InventoryVisible(true);
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


    		private void PlayersTurn()
    		{
    			_damageButton!.Visible = true;
    		}
    	}
}
