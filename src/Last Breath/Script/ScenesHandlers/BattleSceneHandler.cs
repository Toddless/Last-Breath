namespace Playground.Script.ScenesHandlers
{
    using Godot;

    public partial class BattleSceneHandler : Node2D
    {
        private BattleUI? _ui;

        public override void _Ready()
        {
            //var root = GetNode<CanvasLayer>("CanvasLayer");
            //_ui = GetNode<BattleUI>(nameof(BattleUI));
        }

        //public void Init(Player player, BaseEnemy enemy)
        //{
        //    _player = player;
        //    _enemy = enemy;
        //    _player.CanMove = false;
        //    _enemy.EnemyFight = true;
        //    _player.Position = new Vector2(250, 450);
        //    _enemy.Position = new Vector2(950, 450);
        //}


        //private void StartFight()
        //{
        //    if (_enemy == null)
        //    {
        //        SetPlayerStats();
        //        this.CallDeferred("remove_child", _player!);
        //        GetParent().CallDeferred("add_child", _player!);
        //        this.QueueFree();
        //    }
        //}



        //private void EnemyMakeTurn()
        //{
        //    if (_enemy!.HealthComponent!.CurrentHealth > 0)
        //    {
        //        EnemyTurnHandler();
        //        EmitSignal(SignalName.PlayerTurn);
        //    }
        //    else
        //    {
        //        EnemyDead();
        //    }
        //}

        //private void PlayerMakeTurn()
        //{
        //    if (_enemy!.HealthComponent!.CurrentHealth <= 0)
        //    {
        //        EnemyDead();
        //        return;
        //    }
        //    //_attackButtonsUI?.Show();
        //    // var (damage, crit, leeched) = _player!.PlayerAttack!.CalculateDamage();
        //    // TODO: Player attack animation
        //    // _enemy!.HealthComponent!.CurrentHealth -= damage;

        //    //if (crit)
        //    //{
        //    //    // TODO: Crit animation or some nice text 
        //    //    GD.Print("Crit!");
        //    //}
        //    UpdateHealthBar();
        //    //_damageButton!.Visible = false;
        //    //  _attackButtonsUI?.Hide();
        //    EmitSignal(SignalName.EnemyTurn);
        //}


        //private void EnemyTurnHandler()
        //{
        //    float additionalAttackChance = Rnd!.RandfRange(0, 1);



        //    var damage2 = Rnd.RandfRange(_enemy!.AttackComponent!.CurrentMinDamage, _enemy.AttackComponent.CurrentMaxDamage);
        //    if (_enemy.AttackComponent.CurrentCriticalChance <= Rnd.RandfRange(0, 1))
        //    {
        //        damage2 *= _enemy.AttackComponent.CurrentCriticalDamage;
        //    }


        //    var (damage, crit, leeched) = _enemy.ActivateAbilityBeforeDealDamage();

        //    // TODO: Ability animation

        //    // _player!.PlayerHealth!.CurrentHealth -= damage;
        //    if (crit)
        //    {
        //        // TODO: Crit animation or some nice text 
        //        GD.Print("Crit!");
        //    }

        //    UpdateHealthBar();
        //    // _enemy.HealthComponent.Heal(leeched);

        //    GD.Print($"dealed damage {damage}");
        //    //if (additionalAttackChance <= _enemy.AttackComponent!.BaseAdditionalAttackChance)
        //    //{
        //    //    EnemyTurnHandler();
        //    //}
        //}

        //private void PlayerClosedInventory()
        //{
        //    BattleFinished();
        //}

        //private void PlayerTakesAllItems()
        //{
        //    var enemyItems = _enemyInventory?.GiveAllItems();
        //    if (enemyItems?.Count > 0)
        //        _playerInventory?.TakeAllItems(enemyItems);
        //    BattleFinished();
        //}

        //private void BattleFinished()
        //{
        //    SetPlayerStats();
        //    UnsubscribeEvents();
        //    this.CallDeferred("remove_child", _player!);
        //    GetParent().CallDeferred("add_child", _player!);
        //    EmitSignal(SignalName.BattleSceneFinished, _enemy!);
        //}

        //private void UnsubscribeEvents()
        //{
        //    if (_enemy == null)
        //        return;
        //    _returnButton!.Pressed -= BattleFinished;
        //    PlayerTurn -= PlayerMakeTurn;
        //    EnemyTurn -= EnemyMakeTurn;
        //    // _takeAll!.Pressed -= PlayerTakesAllItems;
        //}

        //private void SetPlayerStats()
        //{
        //    _player!.Position = _player.PlayerLastPosition;
        //    _player.CanMove = true;
        //}

        //private void EnemyDead()
        //{
        //    // _attackButtonsUI?.Hide();
        //    _enemyInventory?.ShowInventory?.Invoke();
        //    _enemy?.BattleBehavior?.BattleEnds();
        //}

    }
}
