namespace Playground.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script;
    using Playground.Script.Passives;
    using Playground.Script.Passives.Interfaces;

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
        private Player? _player;
        private IAbility? _activatedAbility;
        private List<IAbility>? _activatedAbilities = [];
        private IAbility? _abitilyWithEffectAfterAttack;


        public BattleBehavior(BaseEnemy enemy)
        {
            _enemyBase = enemy;
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
            foreach (var item in _enemyBase.Abilities)
            {
                if (item.TargetType == typeof(BaseEnemy))
                {
                    item.SetTargetCharacter(_enemyBase);
                }
                if (item.TargetType == typeof(Player))
                {
                    item.SetTargetCharacter(_player);
                }
            }
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

            if (diff > 10)
            {
                if (_iCanDealDamage && enemyHealthPercent > playerHealthPercent)
                {
                }
                if (_iCanHeal && enemyHealthPercent < playerHealthPercent)
                {
                }
            }
            if (diff < 10)
            {
                if (_iCanBuffAttack && enemyHealthPercent > playerHealthPercent)
                {
                }
                if (_iCanLeech && enemyHealthPercent < playerHealthPercent)
                {
                }
            }
            return null;
        }

        #region X
        //private IAbility? UseAbility(Func<IAbility, bool> filter)
        //{
        //    var abilities = _enemyBase!.Abilities!.Where(filter).ToList();
        //    return ActivateAbility(abilities);
        //}

        //private IAbility? BuffYourSelf()
        //{
        //    return UseAbility(x => x is AttackComponent && x is ICanBuffAttack);
        //}

        //private IAbility? TryToKillHimHard()
        //{
        //    return UseAbility(x => x is ICanDealDamage);
        //}

        //private IAbility? TryDealDamageAndHeal()
        //{
        //    return UseAbility(x => x is ICanLeech);
        //}

        //private IAbility? TryToHillYourself()
        //{
        //    return UseAbility(x => x is ICanHeal);
        //}

        //private IAbility? ChoseRandomAbilityNotOnCooldown()
        //{
        //    return UseAbility(x => x.Cooldown == 4);
        //}

        //private IAbility? ActivateAbility(List<IAbility>? abilities)
        //{
        //    if (abilities == null)
        //    {
        //        return null;
        //    }
        //    var amountAbilities = abilities.Where(x => x.Cooldown == 4).Count();
        //    if (amountAbilities == 0)
        //    {
        //        return null;
        //    }

        //    var ability = abilities[Rnd!.RandiRange(0, amountAbilities - 1)];
        //    _activatedAbilities!.Add(ability);
        //    _activatedAbility = ability;
        //    return ability;
        //}

        #endregion

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
                ability.BuffLasts--;
                if (ability.BuffLasts == 0)
                {
                    buffToDelete.Add(ability);
                }
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
            if (_activatedAbility!.BuffLasts != 0)
            {
                return;
            }
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
