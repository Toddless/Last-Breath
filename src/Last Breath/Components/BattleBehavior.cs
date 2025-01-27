namespace Playground.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Passives.Attacks;
    using Playground.Script.Passives.Interfaces;

    // TODO: Need to rework completely
    [Inject]
    public class BattleBehavior
    {
        private bool _iCanHeal;
        private bool _iCanBuffAttack;
        private bool _iCanLeech;
        private bool _iCanDealDamage;
        private RandomNumberGenerator? _rnd;
        private HealthComponent? _playerHealth;
        private BaseEnemy? _enemyBase;
        private ICharacter? _selfTarget;
        private ICharacter? _opponentTarget;
        private Player? _player;
        private IAbility? _activatedAbility;
        private List<IAbility>? _activatedAbilities = [];
        private IAbility? _abitilyWithEffectAfterAttack;


        public BattleBehavior(BaseEnemy enemy)
        {
            _enemyBase = enemy;
            _selfTarget = enemy;
            FindOutWhatICan(_enemyBase.Abilities);
            DiContainer.InjectDependencies(this);
        }

        public IAbility? AbilityWithEffectAfterAttack
        {
            get => _abitilyWithEffectAfterAttack;
        }

        [Inject]
        protected RandomNumberGenerator? Rnd
        {
            get => _rnd;
            set => _rnd = value;
        }

        public void GatherInfo(Player player)
        {
            _player = player;
            _opponentTarget = player;
        }



        public IAbility? MakeDecision()
        {
            UpdateUsedAbilitiesCooldown();
            ReduceBuffDuration();
            RemoveBuffFromLastTurn();
            _abitilyWithEffectAfterAttack = null;
            var enemyHealthPercent = _enemyBase!.HealthComponent!.CurrentHealth / _enemyBase.HealthComponent.MaxHealth * 100;
            var playerHealthPercent = _playerHealth!.CurrentHealth / _playerHealth.MaxHealth * 100;
            var diff = playerHealthPercent - enemyHealthPercent;

            return null;
        }

        private void UpdateUsedAbilitiesCooldown()
        {
            if (_activatedAbilities!.Count == 0)
                return;
            var buffToDelete = new List<IAbility>();
            foreach (var ability in _activatedAbilities)
            {
                ability.Cooldown--;
                if (ability.Cooldown == 0)
                {
                    ability.Cooldown = 4;
                    buffToDelete.Add(ability);
                }
            }
            DeleteBuffsFromList(buffToDelete);
        }


        private void ReduceBuffDuration()
        {
            var buffToDelete = new List<IAbility>();
            foreach (var ability in _activatedAbilities!)
            {
                buffToDelete.Add(ability);
            }
            DeleteBuffsFromList(buffToDelete);
        }


        private void DeleteBuffsFromList(List<IAbility>? abilities)
        {
            foreach (var ability in abilities)
            {
                _activatedAbilities?.Remove(ability);
            }
        }

        private void RemoveBuffFromLastTurn()
        {
            foreach (var item in _activatedAbilities!)
            {
                // item.AfterBuffEnds();
            }
        }

        public void RemoveBuffEffectAfterTurnsEnd()
        {
        }

        public void BattleEnds()
        {
            _playerHealth = null;
            _activatedAbilities = null;
            _enemyBase = null;
        }

        private void FindOutWhatICan(List<IAbility>? abilities)
        {
            if (abilities == null)
            {
                return;
            }
            _iCanHeal = abilities.Any(x => x is ICanHeal);
            _iCanBuffAttack = abilities.Any(x => x is ICanBuffAttack);
            _iCanLeech = abilities.Any(x => x is ICanLeech);
            _iCanDealDamage = abilities.Any(x => x is ICanDealDamage);
        }
    }
}
