namespace Playground.Script.UI
{
    using Godot;
    using System;
    using Playground.Script.Enums;
    using Playground.Script.ScenesHandlers;
    using System.Linq;
    using Playground.Script.BattleSystem;

    public partial class BattleLayer : CanvasLayer
    {
        private RandomNumberGenerator _rnd = new();
        private BattleHandler? _battleHandler;
        [Export] private BattleUI? _battleUI;
        private Player? _player;
        public event Action? BattleEnds;

        public override void _Ready()
        {
            _battleHandler = new BattleHandler();
            SetupEvents();
        }

        public void Init(BattleContext context)
        {
            HandleFightStart(context);
            _battleHandler?.Init(context);
            CallDeferred(nameof(StartBattleOnReady));
        }

        public void StartBattleOnReady() => _battleHandler?.BattleStart();

        private void SetupEvents()
        {
            if (_battleHandler == null || _battleUI == null)
            {
                // TODO: Log
                return;
            }

            _battleHandler.StartTurn += OnTurnStart;
            _battleHandler.EndTurn += OnTurnEnd;
            _battleHandler.BattleEnd += OnBattleEnd;
            _battleUI.HeadButtonPressed += OnHeadButtonPressed;
            _battleUI.EnemyAreaPressed += OnEnemyAreaPressed;
            _battleUI.PlayerAreaPressed += OnPlayerAreaPressed;
            _battleUI.Return += OnReturnPressed;
            _battleUI.DexterityStance += OnDexterityStance;
            _battleUI.StrengthStance += OnStrengthStance;
            _battleUI.IntelligenceStance += OnIntelligenceStance;
        }

        private void OnHeadButtonPressed()
        {
            var target = _player.Target;
            if (target is not Player && target != null)
            {
                // При нажатии я вызвал метод атаки, проатаковал и закончил
                // перейдя на некст фазу, когда атака уже завершена
                // на этом моменте FSM входит в фазу проведения атак и никогда ее не покинет,
                // поскольку атака завершена ДО момента подписки на эвент AllAttacksFinished атакующего
                _player.CurrentStance?.OnAttack(target);
                _player.NextPhase?.Invoke();
            }
        }

        private void OnEnemyAreaPressed()
        {
            // TODO: Only for test
            _player.Target = _battleHandler?.Fighters.FirstOrDefault(x => x is not Player);
        }

        private void OnPlayerAreaPressed()
        {
            // TODO: Only for test
            _player.Target = _player;
        }

        private void OnTurnEnd()
        {
            if (IsPlayerTurn())
                _battleUI?.HideButtons();
        }

        private void OnTurnStart()
        {
            if (IsPlayerTurn())
                _battleUI?.ShowButtons();
        }

        private bool IsPlayerTurn()
        {
            var type = _battleHandler?.GetTypeOfCurrentAttacker();
            if (type == null)
                return false;
            return type == typeof(Player);
        }

        private void OnBattleEnd(BattleResults results)
        {
            var fighter = _battleHandler.Fighters.FirstOrDefault(x => x is not Player);
            if (fighter != null)
            {
                UnsubscribeEnemyElements(fighter);
            }

            _battleHandler.CleanBattleHandler();
        }

        private void SubscribeNewStance()
        {
            _player.CurrentStance!.CurrentResourceChanges += _battleUI!.OnPlayerCurrenResourceChanges;
            _player.CurrentStance.MaximumResourceChanges += _battleUI.OnPlayerMaxResourceChanges;
            _battleUI.SetPlayerResource(_player.CurrentStance.Resource.ResourceType, _player.CurrentStance.Resource.Current, _player.CurrentStance.Resource.MaximumAmount);
        }

        private void UnsubscribeOldStance()
        {
            // im sure we have some stance here
            _player.CurrentStance!.CurrentResourceChanges -= _battleUI!.OnPlayerCurrenResourceChanges;
            _player.CurrentStance.MaximumResourceChanges -= _battleUI.OnPlayerMaxResourceChanges;
        }

        private void OnIntelligenceStance()
        {

        }

        private void OnStrengthStance()
        {
            HandleOldStance();

            _player.SetStrengthStance();

            SubscribeNewStance();
        }

        private void OnDexterityStance()
        {
            HandleOldStance();

            _player.SetDexterityStance();

            SubscribeNewStance();
        }

        private void HandleOldStance()
        {
            if (HaveAnyCurrentStance())
            {
                UnsubscribeOldStance();
            }
        }

        private bool HaveAnyCurrentStance() => _player.CurrentStance != null;

        private void OnReturnPressed()
        {
            // TODO: chance to escape depend on player an enemy power
            if (_rnd.Randf() <= 0.5f)
            {
                _battleHandler?.PlayerEscapedBattle();
            }
            else
            {
                _battleHandler?.PlayerFailToEscapeBattle();
            }
        }

        private void HandleFightStart(BattleContext context)
        {
            _player = GameManager.Instance.Player;
            // for test i have only one scenario, where we have player and one enemy
            // for multiple enemies i need another method and logic for UI
            // Нужно разместить в UI так что бы гарантировать что персонажи корректно отпишутся от эвентов
            foreach (var fighter in context.Fighters)
            {
                if (fighter is Player)
                    continue;
                SubscribeEnemyElements(fighter);
            }
            SubscribePlayerElements(_player);
        }

        private void SubscribeEnemyElements(ICharacter enemy)
        {
            _battleUI?.SetEnemyHealthBar(enemy.Health.CurrentHealth, enemy.Health.MaxHealth);
            _battleUI?.SetEnemyResource(enemy.CurrentStance!.Resource.ResourceType, enemy.CurrentStance.Resource.Current, enemy.CurrentStance.Resource.MaximumAmount);
            enemy.Health.CurrentHealthChanged += _battleUI.OnEnemyCurrentHealthChanged;
            enemy.Health.MaxHealthChanged += _battleUI.OnEnemyMaxHealthChanged;
            enemy.CurrentStance!.CurrentResourceChanges += _battleUI!.OnEnemyCurrentResourceChanges;
            enemy.CurrentStance.MaximumResourceChanges += _battleUI.OnEnemyMaxResourceChanges;
        }

        private void UnsubscribeEnemyElements(ICharacter enemy)
        {
            enemy.Health.CurrentHealthChanged -= _battleUI.OnEnemyCurrentHealthChanged;
            enemy.Health.MaxHealthChanged -= _battleUI.OnEnemyMaxHealthChanged;
            enemy.CurrentStance!.CurrentResourceChanges -= _battleUI!.OnEnemyCurrentResourceChanges;
            enemy.CurrentStance.MaximumResourceChanges -= _battleUI.OnEnemyMaxResourceChanges;
        }

        private void SubscribePlayerElements(Player player)
        {
            _battleUI?.SetPlayerHealthBar(player.Health.CurrentHealth, player.Health.MaxHealth);
            player.Health.CurrentHealthChanged += _battleUI.OnPlayerCurrentHealthChanged;
            player.Health.MaxHealthChanged += _battleUI.OnPlayerMaxHealthChanged;
        }
    }
}
