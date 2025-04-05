namespace Playground.Script.ScenesHandlers
{
    using System;
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Stateless;

    public partial class BattleSceneHandler : ObservableProperty
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
        private ICharacter? _attacker, _defender;

        public event Action<BattleResult>? BattleEnd;

        public BattleSceneHandler()
        {
            ConfigureStateMachine();
        }

        public void Init(BattleContext context)
        {
            // TODO: Rework context
            _player = (Player)context.Player;
            _enemy = (BaseEnemy)context.Opponent;
            DecideFirstTurn();
        }

        public void PlayerTryingToRunAway()
        {
            if (!(_rnd.RandfRange(0, 1) <= ChanceToEscape)) return;
            // need method to refresh enemy stats
            BattleEnd?.Invoke(BattleFinished(BattleResults.PlayerRunAway));
            _machine?.Fire(Trigger.Await);
        }

        public void DexterityStance()
        {
            _player!.Stance = Enums.Stance.Dexterity;
            GD.Print($"Player stance: {_player.Stance}");
        }

        public void StrengthStance()
        {
            _player!.Stance = Enums.Stance.Strength;
            GD.Print($"Player stance: {_player.Stance}");
        }

        public void PlayerTurn()
        {
            TryAttack();
            _machine.Fire(Trigger.EnemyTurn);
        }

        private void EnemyMakesTurn()
        {
            TryAttack();
            _machine.Fire(Trigger.PlayerTurn);
        }

        private void ConfigureStateMachine()
        {
            _machine.Configure(State.Awaiting)
                .OnEntry(() =>
                {
                    ClearBattleScene();
                })
                .Permit(Trigger.PlayerTurn, State.Player)
                .Permit(Trigger.EnemyTurn, State.Enemy);

            _machine.Configure(State.Player)
                .OnEntry(() =>
                {
                    GD.Print("Player turn");
                })
                .OnExit(() =>
                {
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
                case Enums.Stance.Dexterity:
                    SwitchRoles();
                    TryCounterAttack();
                    break;
                case Enums.Stance.Strength:
                    break;
                case Enums.Stance.Intelligence:
                    break;
            }
        }

        private void TryCounterAttack()
        {
            GD.Print($"{GetCharacterName(_attacker)} performing counter attack");
            bool additionalHit;
            do
            {
                if (!PerformAttack())
                {
                    break;
                }
                additionalHit = IsAdditionalHit(_attacker);

            } while (additionalHit);
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
