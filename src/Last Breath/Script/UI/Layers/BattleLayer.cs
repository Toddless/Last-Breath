namespace LastBreath.Script.UI
{
    using Godot;
    using System;
    using System.Linq;
    using LastBreath.Script;
    using LastBreath.Script.Helpers.Extensions;
    using LastBreath.Script.ScenesHandlers;
    using LastBreath.Script.BattleSystem;
    using Contracts.Enums;

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
            _battleHandler.OnEnterStartPhase += OnEnterStartPhase;
            _battleHandler.OnExitStartPhase += OnExitStartPhase;
            _battleHandler.BattleEnd += OnBattleEnd;
            _battleUI.HeadButtonPressed += OnHeadButtonPressed;
            _battleUI.EnemyAreaPressed += OnEnemyAreaPressed;
            _battleUI.PlayerAreaPressed += OnPlayerAreaPressed;
            _battleUI.Return += OnReturnPressed;
            _battleUI.DexterityStance += OnDexterityStance;
            _battleUI.StrengthStance += OnStrengthStance;
            _battleUI.IntelligenceStance += OnIntelligenceStance;
        }

        private async void OnTurnStart(ICharacter character)
        {
            var notifierScene = TurnNotifier.InitializeAsPackedScene();
            var notifier = notifierScene.Instantiate<TurnNotifier>();
            AddChild(notifier);
            notifier.ShowMessage(character);

            if (character is not Player)
            {
                await ToSignal(notifier, SignalNameExtension.Completed);
                character.OnTurnStart();
            }
        }

        // Only for test
        private void OnHeadButtonPressed()
        {
            var target = _player?.Target;
            if (target is not Player && target != null)
            {
                _player?.CurrentStance?.OnAttack(target);
                UIEventBus.PublishNextPhase();
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

        private void OnExitStartPhase()
        {
            _battleUI?.HideButtons();
        }

        private void OnEnterStartPhase()
        {
            if (!IsPlayerTurn())
            {
                _battleUI?.HideButtons();
            }
            else
            {
                _battleUI?.ShowButtons();
            }
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
            // TODO: Rework this. Later here will be multiple enemies
            var fighter = _battleHandler?.Fighters.FirstOrDefault(x => x is not Player);
            if (fighter != null)
            {
                UnsubscribeEnemyElements(fighter);
            }
            UnsubscribePlayerElements(_player);
            _battleHandler?.CleanBattleHandler();
            CallDeferred(nameof(ReturnOnMainUI));
        }

        private void ReturnOnMainUI() => BattleEnds?.Invoke();

        private void OnIntelligenceStance()
        {
            HandleOldStance();
            _player?.SetIntelligenceStance();
            SubscribeNewPlayerStance();
        }

        private void OnStrengthStance()
        {
            HandleOldStance();
            _player?.SetStrengthStance();
            SubscribeNewPlayerStance();
        }

        private void OnDexterityStance()
        {
            HandleOldStance();
            _player?.SetDexterityStance();
            SubscribeNewPlayerStance();
        }

        private void HandleOldStance()
        {
            if (HaveAnyCurrentStance())
            {
                UnsubscribeOldPlayerStance();
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
            enemy.GettingAttack += OnDamageTaken;
        }

        private void SubscribePlayerElements(Player player)
        {
            _battleUI?.SetPlayerHealthBar(player.Health.CurrentHealth, player.Health.MaxHealth);
            player.Health.CurrentHealthChanged += _battleUI.OnPlayerCurrentHealthChanged;
            player.Health.MaxHealthChanged += _battleUI.OnPlayerMaxHealthChanged;
            player.GettingAttack += OnDamageTaken;
        }

        private void OnDamageTaken(OnGettingAttackEventArgs args) => _battleUI?.OnGettingAttack(args);

        private void SubscribeNewPlayerStance()
        {
            if (_player != null)
            {
                _player.CurrentStance!.CurrentResourceChanges += _battleUI!.OnPlayerCurrenResourceChanges;
                _player.CurrentStance.MaximumResourceChanges += _battleUI.OnPlayerMaxResourceChanges;
                _battleUI.SetPlayerResource(_player.CurrentStance.Resource.ResourceType, _player.CurrentStance.Resource.Current, _player.CurrentStance.Resource.MaximumAmount);
            }
        }

        private void UnsubscribeOldPlayerStance()
        {
            if (_player != null)
            {
                _player.CurrentStance!.CurrentResourceChanges -= _battleUI!.OnPlayerCurrenResourceChanges;
                _player.CurrentStance.MaximumResourceChanges -= _battleUI.OnPlayerMaxResourceChanges;
            }
        }

        private void UnsubscribeEnemyElements(ICharacter enemy)
        {
            enemy.Health.CurrentHealthChanged -= _battleUI.OnEnemyCurrentHealthChanged;
            enemy.Health.MaxHealthChanged -= _battleUI.OnEnemyMaxHealthChanged;
            enemy.CurrentStance!.CurrentResourceChanges -= _battleUI!.OnEnemyCurrentResourceChanges;
            enemy.CurrentStance.MaximumResourceChanges -= _battleUI.OnEnemyMaxResourceChanges;
            enemy.GettingAttack -= OnDamageTaken;
        }

        private void UnsubscribePlayerElements(Player player)
        {
            player.Health.CurrentHealthChanged -= _battleUI.OnPlayerCurrentHealthChanged;
            player.Health.MaxHealthChanged -= _battleUI.OnPlayerMaxHealthChanged;
            player.GettingAttack -= OnDamageTaken;
        }
    }
}
