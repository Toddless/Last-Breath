namespace Playground.Script.UI
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.ScenesHandlers;

    public partial class BattleLayer : CanvasLayer
    {
        private BattleSceneHandler? _battleSceneHandler;
        [Export] private BattleUI? _battleUI;

        public event Action? BattleEnds;

        public override void _Ready()
        {
            _battleSceneHandler = new BattleSceneHandler();
            SetupEvents();
        }

        public void Init(BattleContext context)
        {
            HandleFightStart(context);
            _battleSceneHandler?.Init(context);
        }

        private void SetupEvents()
        {
            _battleUI!.DexterityStance += () => _battleSceneHandler?.DexterityStance();
            _battleUI.StrengthStance += () => _battleSceneHandler?.StrengthStance();
            _battleUI!.HeadButtonPressed += () => _battleSceneHandler?.PlayerTurn();

            _battleSceneHandler!.ShowAttackButtons += _battleUI.ShowAttackButtons;
            _battleSceneHandler!.HideAttackButtons += _battleUI.HideAttackButtons;
            _battleSceneHandler!.DamageDealed += _battleUI.OnDamageDealed;
            _battleSceneHandler.BattleEnd += OnBattleEnds;

            _battleUI!.Return = _battleSceneHandler.PlayerTryingToRunAway;
        }

        private void HandleFightStart(BattleContext context)
        {
            var player = (Player)context.Player;
            var enemy = (BaseEnemy)context.Opponent;
            context.Player.CanFight = false;
            context.Player.CanMove = false;
            context.Opponent.CanFight = false;
            context.Opponent.CanMove = false;
            // setup players ability in subscribeBattleUi or SetAbilities?
            SubscribeToBattleUI(player, enemy);
            // adding to UI Player and Enemy Animatio1ns
            // i just set as default target an enemy
            GD.Print($"Enemy current Resource: {enemy.Resource.GetCurrentResource()}");
            SetAbilities(player);
        }

        private void SubscribeToBattleUI(Player player, BaseEnemy enemy)
        {
            _battleUI!.PlayerAreaPressed += () => player.Target = player;
            _battleUI!.EnemyAreaPressed += () => player.Target = enemy;
            SubscribePlayerElements(player);
            SubscribeEnemyElements(enemy);
        }

        private void SubscribeEnemyElements(BaseEnemy enemy)
        {
            _battleUI?.SetEnemyHealthBar(enemy.Health.CurrentHealth, enemy.Health.MaxHealth);
            _battleUI?.SetEnemyResource(enemy.Resource.GetCurrentResource(), enemy.Resource.CurrentResource.Current, enemy.Resource.CurrentResource.MaximumAmount);
            GD.Print($"Set enemy current resource: {enemy.Resource.CurrentResource.Current}");
            enemy.Resource.CurrentResourceValueChanges += _battleUI!.OnEnemyCurrentResourceChanges;
            enemy.Health.CurrentHealthChanged += _battleUI.OnEnemyCurrentHealthChanged;
            enemy.Health.MaxHealthChanged += _battleUI.OnEnemyMaxHealthChanged;
        }

        private void SubscribePlayerElements(Player player)
        {
            _battleUI?.SetPlayerHealthBar(player.Health.CurrentHealth, player.Health.MaxHealth);
            _battleUI?.SetPlayerResource(player.Resource.GetCurrentResource(), player.Resource.CurrentResource.Current, player.Resource.CurrentResource.MaximumAmount);
            GD.Print($"Set player current resource: {player.Resource.CurrentResource.Current}");
            player.SetAvailableAbilities += OnNewSetAbility;
            player.Resource.CurrentResourceValueChanges += _battleUI!.OnPlayerCurrenResourceChanges;
            player.Resource.CurrentResourceTypeChanges += _battleUI.SetPlayerResource;
            player.Health.CurrentHealthChanged += _battleUI.OnPlayerCurrentHealthChanged;
            player.Health.MaxHealthChanged += _battleUI.OnPlayerMaxHealthChanged;
        }

        private void OnNewSetAbility(List<IAbility> list) => list.ForEach(ability => _battleUI?.SetAbility(ability));

        public void UnsubscribeBattleUI(Player player, BaseEnemy enemy)
        {
            player.Health.CurrentHealthChanged -= _battleUI!.OnPlayerCurrentHealthChanged;
            player.Health.MaxHealthChanged -= _battleUI.OnPlayerMaxHealthChanged;
            player.Resource.CurrentResourceValueChanges -= _battleUI.OnPlayerCurrenResourceChanges;
            player.Resource.CurrentResourceTypeChanges -= _battleUI.SetPlayerResource;
            player.SetAvailableAbilities -= OnNewSetAbility;

            enemy.Health.CurrentHealthChanged -= _battleUI.OnEnemyCurrentHealthChanged;
            enemy.Health.MaxHealthChanged -= _battleUI.OnEnemyMaxHealthChanged;
            enemy.Resource.CurrentResourceValueChanges -= _battleUI.OnEnemyCurrentResourceChanges;
            _battleUI.ClearAbilities();
        }

        private void SetAbilities(Player player)
        {
            if (player.Stance == Enums.Stance.None) return;
            foreach (var ability in player.Abilities[player.Stance])
            {
                _battleUI?.SetAbility(ability);
            }
        }

        private void OnBattleEnds(BattleResult result)
        {
            var player = result.Player;
            var enemy = result.Enemy;
            switch (result.Results)
            {
                case Enums.BattleResults.EnemyWon:
                    HandleEnemyWon(result);
                    break;
                case Enums.BattleResults.PlayerWon:
                    HandleEnemyKilled(enemy, player);
                    break;
                case Enums.BattleResults.PlayerRunAway:
                    HandlePlayerRunAway(result);
                    break;
            }
            UnsubscribeBattleUI((Player)result.Player, (BaseEnemy)result.Enemy);
            BattleEnds?.Invoke();
        }

        private void HandleEnemyKilled(ICharacter enemy, ICharacter player)
        {
            if (enemy is BaseEnemy baseEnemy && player is Player p)
            {
                p.OnEnemyKilled(baseEnemy);
                p.CanMove = true;
                p.CanFight = true;
            }
        }

        private void HandleEnemyWon(BattleResult result) => HandlePlayerRunAway(result);

        private void HandlePlayerRunAway(BattleResult result)
        {
            var player = (Player)result.Player;
            var enemy = (BaseEnemy)result.Enemy;
            player.OnRunAway(enemy.Position);
            enemy.CanMove = true;
            enemy.CanFight = true;
        }
    }
}
