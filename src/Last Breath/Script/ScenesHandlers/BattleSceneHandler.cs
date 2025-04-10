namespace Playground.Script.ScenesHandlers
{
    using System;
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Stateless;

    public partial class BattleSceneHandler
    {
        private enum State { Player, Enemy, Awaiting }
        private enum Trigger { PlayerTurn, EnemyTurn, Await }

        private readonly StateMachine<State, Trigger> _machine = new(State.Awaiting);

        public event Action? HideAttackButtons, ShowAttackButtons;
        // Rework later. This chance should depends on some stats (e.g. if player has lower chance to escape, if enemy has more levels)
        private const float ChanceToEscape = 0.5f;
        private readonly RandomNumberGenerator _rnd = new();
        private Player? _player;
        private BaseEnemy? _enemy;
        private ICharacter? _attacker, _defender, _playerTarget;
        private bool _isPlayerTurn = false;

        public event Action<BattleResult>? BattleEnd;
        public event Action<ICharacter?>? TargetChanges;
        public event Action? PlayerTurnEnds;

        public ICharacter? Target
        {
            get => _playerTarget;
            set
            {
                // TODO: What do i do on enemy turn? Can player still switch targets?
                if (ObservableProperty.SetProperty(ref _playerTarget, value))
                    TargetChanges?.Invoke(_playerTarget);
            }
        }

        public BattleSceneHandler()
        {
            ConfigureStateMachine();
        }

        public void Init(BattleContext context)
        {
            // TODO: Rework context, if i will let enemies fighting each other on this layer
            _player = (Player)context.Player;
            _enemy = (BaseEnemy)context.Opponent;
            DecideFirstTurn();
        }

        public void PlayerTryingToRunAway()
        {
            // instead i should hide this button on enemy turn
            if (_machine.State == State.Enemy) return;
            if (!(_rnd.RandfRange(0, 1) <= ChanceToEscape))
            {
                _machine.Fire(Trigger.EnemyTurn);
                return;
            }
            // need method to refresh enemy stats
            BattleEnd?.Invoke(BattleFinished(BattleResults.PlayerRunAway));
            _machine?.Fire(Trigger.Await);
        }

        public void DexterityStance()
        {
            _player!.Stance = Stance.Dexterity;
            GD.Print($"Player stance: {_player.Stance}");
        }

        public void StrengthStance()
        {
            _player!.Stance = Stance.Strength;
            GD.Print($"Player stance: {_player.Stance}");
        }

        public void OnEnemyAreaPressed()
        {
            if(!_isPlayerTurn) return;
            Target = _enemy;
        }

        public void OnPlayerAreaPressed()
        {
            if (!_isPlayerTurn) return;
            Target = _player;
        }

        public void PlayerTurn()
        {
            // TODO: In Strenght stance player and enemy cannot attack multiple times
            TryAttack();
            // Method for resource generation
            // TODO: On end turn give player abilities EXP
            _machine.Fire(Trigger.EnemyTurn);
        }

        private void EnemyMakesTurn()
        {
            // Decide ability to use
            // check if enough resources and cast second ability if needed (need to create system for that, so enemy do not use some ability, even though he has resources)
            DecideAbility();
            TryAttack();
            // Method for resource generation
            _machine.Fire(Trigger.PlayerTurn);
        }

        private void DecideAbility()
        {

        }

        private void ConfigureStateMachine()
        {
            _machine.Configure(State.Awaiting)
                .OnEntry(ClearBattleScene)
                .Permit(Trigger.PlayerTurn, State.Player)
                .Permit(Trigger.EnemyTurn, State.Enemy);

            _machine.Configure(State.Player)
                .OnEntry(() =>
                {
                    _isPlayerTurn = true;
                    GD.Print("Player turn");
                })
                .OnExit(() =>
                {
                    PlayerTurnEnds?.Invoke();
                    _isPlayerTurn = false;
                    _player?.Effects.UpdateEffects();
                    SwitchRoles();
                    GD.Print("Player turn ends");
                })
                .Permit(Trigger.Await, State.Awaiting)
                .Permit(Trigger.EnemyTurn, State.Enemy);

            _machine.Configure(State.Enemy)
                .OnEntry(() =>
                {
                    HideAttackButtons?.Invoke();
                    GD.Print("Enemy turn");
                    EnemyMakesTurn();
                })
                .OnExit(() =>
                {
                    _enemy?.Effects.UpdateEffects();
                    SwitchRoles();
                    GD.Print("Enemy turn ends");
                    ShowAttackButtons?.Invoke();
                })
                .Permit(Trigger.Await, State.Awaiting)
                .Permit(Trigger.PlayerTurn, State.Player);
        }

        private void TryAttack()
        {
            bool additionalHit;
            do
            {
                if (!PerformAttack())
                {
                    HandleEvadedAttack();
                    break;
                }
                additionalHit = IsAdditionalHit(_attacker);
            } while (additionalHit);
        }

        private bool PerformAttack()
        {
            // _defender cannot evade attack if stunned
            if (IsEvade(_defender))
            {
                GD.Print($"{GetCharacterName(_defender)} evaded attack");
                return false;
            }
            ApplyDamage(CalculateDamage());
            return true;
        }

        private void HandleEvadedAttack()
        {
            GD.Print($"Handling evaded attack. Defender: {GetCharacterName(_defender)}");
            switch (_defender.Stance)
            {
                case Stance.Dexterity:
                    SwitchRoles();
                    TryCounterAttack();
                    break;
                case Stance.Strength:
                    // TODO: Mechanic for stance
                    break;
                case Stance.Intelligence:
                    // TODO: Mechanic for stance
                    break;
            }
        }

        // for later: if this method still remain the same, just add bool with default value to TryAttack
        // and call it in HandleEvadedAttack
        private void TryCounterAttack()
        {
            GD.Print($"{GetCharacterName(_attacker)} performing counter attack");
            TryAttack();
            SwitchRoles();
        }

        private float CalculateDamage()
        {
            float damage = _attacker!.Damage!.GetFlatDamage();
            if (IsCrit(_attacker))
            {
                damage *= _attacker.Damage.GetCriticalDamage();
            }
            return Mathf.Max(0, damage * (1 - Mathf.Min(_defender!.Defense!.GetArmor() / 100, 0.7f)));
        }

        private void ApplyDamage(float damage)
        {
            _defender?.Health!.TakeDamage(damage);
            CheckBattleEnds();
            GD.Print($"Dialed Damage: {damage} to {GetCharacterName(_defender)}");
        }

        private void DecideFirstTurn()
        {
            if (_rnd.Randf() >= 0.51f)
            {
                _attacker = _player;
                _defender = _enemy;
                _machine.Fire(Trigger.PlayerTurn);
            }
            else
            {
                _attacker = _enemy;
                _defender = _player;
                _machine.Fire(Trigger.EnemyTurn);
            }
        }

        private void SwitchRoles()
        {
            if (_attacker == _player && _defender == _enemy)
            {
                _attacker = _enemy;
                _defender = _player;
            }
            else
            {
                _attacker = _player;
                _defender = _enemy;
            }
            GD.Print($"Attacker: {GetCharacterName(_attacker)}, Defender: {GetCharacterName(_defender)}");
        }

        private string GetCharacterName(ICharacter character) => character.GetType().Name;
        private bool IsAdditionalHit(ICharacter? character) => _rnd.RandfRange(1, 2) <= character?.Damage?.GetAdditionalHitChance();
        private bool IsCrit(ICharacter? character) => _rnd.RandfRange(1, 2) <= character?.Damage?.GetCriticalChance();
        private bool IsEvade(ICharacter? character) => _rnd.RandfRange(1, 2) <= character?.Defense?.GetDodgeChance();

        private void CheckBattleEnds()
        {
            if (_player?.Health?.CurrentHealth <= 0)
            {
                BattleEnd?.Invoke(BattleFinished(BattleResults.EnemyWon));
                _machine.Fire(Trigger.Await);
            }
            else if (_enemy?.Health?.CurrentHealth <= 0)
            {
                BattleEnd?.Invoke(BattleFinished(BattleResults.PlayerWon));
                _machine.Fire(Trigger.Await);
            }
        }

        private void ClearBattleScene()
        {
            _player = null;
            _enemy = null;
            _defender = null;
            _attacker = null;
            _playerTarget = null;
        }

        private BattleResult BattleFinished(BattleResults results)
        {
            return results switch
            {
                BattleResults.EnemyWon => new(_player!, _enemy!, BattleResults.EnemyWon),
                BattleResults.PlayerWon => new(_player!, _enemy!, BattleResults.PlayerWon),
                _ => new(_player!, _enemy!, BattleResults.PlayerRunAway),
            };
        }
    }
}
