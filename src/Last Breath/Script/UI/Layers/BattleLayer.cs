namespace Playground.Script.UI
{
    using System;
    using Playground.Script.Helpers;
    using Playground.Script.ScenesHandlers;

    public partial class BattleLayer : ObservableLayer
    {
        private BattleSceneHandler? _battleSceneHandler;
        private BattleUI? _battleUI;

        public event Action? BattleEnds;

        public override void _Ready()
        {
            _battleSceneHandler = new BattleSceneHandler();
            _battleUI = GetNode<BattleUI>(nameof(BattleUI));
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
            _battleUI.HeadButtonPressed += () => _battleSceneHandler?.PlayerTurn();
            _battleSceneHandler!.ShowAttackButtons += _battleUI.ShowAttackButtons;
            _battleSceneHandler!.HideAttackButtons += _battleUI.HideAttackButtons;
            _battleUI!.Return = _battleSceneHandler.PlayerTryingToRunAway;
            _battleSceneHandler.BattleEnd += OnBattleEnds;
            _battleUI!.PlayerAreaPressed += _battleSceneHandler.OnPlayerAreaPressed;
            _battleUI!.EnemyAreaPressed += _battleSceneHandler.OnEnemyAreaPressed;
            _battleSceneHandler.TargetChanges += (t) => _battleUI?.OnTargetChanges(t);
            _battleSceneHandler.PlayerTurnEnds += _battleUI.OnTurnEnds;
        }

        private void HandleFightStart(BattleContext context)
        {
            var player = (Player)context.Player;
            context.Player.CanFight = false;
            context.Player.CanMove = false;
            context.Opponent.CanFight = false;
            context.Opponent.CanMove = false;
            // setup players ability in subscribeBattleUi or SetAbilities?
            _battleUI?.SubscribeBattleUI((Player)context.Player, (BaseEnemy)context.Opponent);
            // adding to UI Player and Enemy Animatio1ns
            // i just set as default target an enemy
            SetAbilities(player);
        }

        private void SetAbilities(Player player)
        {
            foreach (var ability in player.Abilities)
            {
                _battleUI?.SetAbility(ability);
            }
        }

        private void OnBattleEnds(BattleResult result)
        {
            switch (result.Results)
            {
                case Enums.BattleResults.EnemyWon:
                    HandleEnemyWon(result);
                    break;
                case Enums.BattleResults.PlayerWon:
                    HandlePlayerRunAway(result);
                    // HandleEnemyKilled(result.Enemy);
                    break;
                case Enums.BattleResults.PlayerRunAway:
                    HandlePlayerRunAway(result);
                    break;
            }
            _battleUI?.UnsubscribeBattleUI((Player)result.Player, (BaseEnemy)result.Enemy);
            BattleEnds?.Invoke();
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

        private void HandleEnemyKilled(ICharacter enemy)
        {
            // нужен каст к базовому противнику (??)
            // сменить ассет
            // противник не может двигаться
            // противник не может драться
            // игрок может облутать поверженного противника
        }
    }
}
