namespace Playground.Script.UI
{
    using System;
    using Playground.Script.Helpers;
    using Playground.Script.ScenesHandlers;

    public partial class BattleLayer : ObservableLayer
    {
        private BattleSceneHandler? _battleSceneHandler;
        private BattleUI? _battleUI;

        public Action<BattleResult?>? ReturnToMainWorld;
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
        }

        private void HandleFightStart(BattleContext context)
        {
            context.Player.CanFight = false;
            context.Player.CanMove = false;
            context.Opponent.CanFight = false;
            context.Opponent.CanMove = false;
            // adding to UI Player and Enemy Animations
        }

        private void OnBattleEnds(BattleResult result)
        {
            switch (result.Results)
            {
                case Enums.BattleResults.EnemyWon:
                    HandleEnemyWon(result);
                    break;
                    case Enums.BattleResults.PlayerWon:
                    HandleEnemyKilled(result.Enemy);
                    break;
                    case Enums.BattleResults.PlayerRunAway:
                    HandlePlayerRunAway(result);
                    break;
            }
            // данную часть изменить/разделить. Иначе начнется новый бой сразу же после окончания старого
            // может ли игрок в случае поражения драться, пока под вопросом
            BattleEnds?.Invoke();
        }

        private void HandleEnemyWon(BattleResult result)
        {

        }

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
