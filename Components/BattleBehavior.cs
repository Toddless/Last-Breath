namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script.Passives.Attacks;
    using Playground.Script.Passives.Interfaces;

    public class BattleBehavior
    {
        private bool _iCanHeal;
        private bool _iCanBuffAttack;
        private bool _iCanLeech;
        private bool _iCanDealDamage;
        private HealthComponent? _playerHealth;
        private EnemyAI? _enemyAI;
        private List<Ability>? _activatedAbilities = [];
        private Ability? _abitilyWithEffectAfterAttack;

        public BattleBehavior(EnemyAI enemy)
        {
            _enemyAI = enemy;
            FindOutWhatICan(_enemyAI.Abilities);
        }

        public Ability? AbilityWithEffectAfterAttack
        {
            get => _abitilyWithEffectAfterAttack;
        }

        public void GatherInfo(Player player)
        {
            _playerHealth = player.PlayerHealth;
        }

        private void FindOutWhatICan(List<Ability>? abilities)
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

        public Ability? MakeDecision()
        {
            UpdateUsedAbilitiesCooldown();
            RemoveBuffFromLastTurn();
            _abitilyWithEffectAfterAttack = null;
            var enemyHealthPercent = _enemyAI!.HealthComponent!.CurrentHealth / _enemyAI.HealthComponent.MaxHealth * 100;
            var playerHealthPercent = _playerHealth!.CurrentHealth / _playerHealth.MaxHealth * 100;
            var diff = playerHealthPercent - enemyHealthPercent;

            if(diff > 20)
            {
                if(_iCanDealDamage && enemyHealthPercent > playerHealthPercent)
                {
                    return TryToKillHimHard();
                }
                if(_iCanHeal && enemyHealthPercent < playerHealthPercent)
                {
                    return TryToHillYourself();
                }
            }
            if(diff < 20)
            {
                if(_iCanDealDamage && enemyHealthPercent > playerHealthPercent)
                {
                    return BuffYourSelf();
                }
                if (_iCanLeech && enemyHealthPercent < playerHealthPercent)
                {
                    return TryDealDamageAndHeal();
                }
            }

            return ChoseRandomAbilityNotOnCooldown();
        }


        private Ability? UseAbility(Func<Ability, bool> filter)
        {
            var abilities = _enemyAI!.Abilities!.Where(filter).ToList();
            return ActivateAbility(abilities);
        }

        private Ability? BuffYourSelf()
        {
            return UseAbility(x => x is ICanBuffAttack);
        }

        private Ability? TryToKillHimHard()
        {
            return UseAbility(x => x is ICanDealDamage);
        }

        private Ability? TryDealDamageAndHeal()
        {
            return UseAbility(x => x is ICanLeech);
        }

        private Ability? TryToHillYourself()
        {
            return UseAbility(x => x is ICanHeal);
        }

        private Ability? ChoseRandomAbilityNotOnCooldown()
        {
            return UseAbility(x => x.Cooldown == 4);
        }

        private Ability? ActivateAbility(List<Ability>? abilities)
        {
            if(abilities == null)
            {
                return null;
            }
            var amountAbilities = abilities.Where(x=> x.Cooldown == 4).Count();
            GD.Print($"Abilities to activate {amountAbilities}");
            if(amountAbilities == 0)
            {
                return null;
            }

            var ability = abilities[_enemyAI!.Rnd.RandiRange(0, amountAbilities -1)];
            _activatedAbilities!.Add(ability);

            if (ability.HaveISomethinToApplyAfterAttack)
            {
                _abitilyWithEffectAfterAttack = ability;
            }
            return ability;
        }

        private void UpdateUsedAbilitiesCooldown()
        {
            if (_activatedAbilities!.Count == 0)
                return;
            var itemsToRemove = new List<Ability>();
            foreach (var item in _activatedAbilities)
            {
                item.Cooldown--;
                GD.Print($"Activated ability {item.GetType()} cooldown left {item.Cooldown}");
                if(item.Cooldown == 0)
                {
                    item.Cooldown = 4;
                    itemsToRemove.Add(item);
                }
            }

            foreach (var item in itemsToRemove)
            {
               _activatedAbilities.Remove(item);
            }
        }

        private void RemoveBuffFromLastTurn()
        {
            foreach (var item in _activatedAbilities)
            {
                item.AfterBuffEnds(_enemyAI.EnemyAttack);
            }
        }


        public void BattleEnds()
        {
            _playerHealth = null;
            _activatedAbilities = null;
            _enemyAI = null;
        }
    }
}
