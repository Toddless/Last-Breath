namespace Playground.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Script.Passives.Attacks;
    using Playground.Script.Passives.Interfaces;

    public class BattleBehavior
    {
        private List<Ability>? _abilitiesToChoseFrom;
        private bool _iCanHeal;
        private bool _iCanBuff;
        private HealthComponent? _health;
        private HealthComponent? _playerHealth;
        private EnemyAI? _enemyAI;

        public BattleBehavior(HealthComponent health, HealthComponent playerHealth, EnemyAI enemy, List<Ability>? abilities)
        {
            _health = health;
            _playerHealth = playerHealth;
            _enemyAI = enemy;
            FindOutWhatICan(abilities);
        }

        private void FindOutWhatICan(List<Ability>? abilities)
        {
            foreach (var item in abilities!)
            {
                if (item is ICanHeal)
                {
                    _iCanHeal = true;
                }
                if (item is ICanBuffAttack)
                {
                    _iCanBuff = true;
                }
            }
        }

        public void SomeTest()
        {
            var currentHealthPercent = _health!.CurrentHealth / _health.MaxHealth * 100;
            var playerHealthPercent = _playerHealth!.CurrentHealth / _playerHealth.MaxHealth * 100;
            if (currentHealthPercent > playerHealthPercent && _iCanBuff)
            {
                BuffYourSelf();
            }
            else if (currentHealthPercent < playerHealthPercent && _iCanHeal)
            {
                TryToHillYourself();
            }
            else if (playerHealthPercent < 30)
            {
                TryToKillHimHard();
            }
            else
            {
                ChoseRandomAbility();
            }
        }

        private void BuffYourSelf()
        {
            var abilities = _abilitiesToChoseFrom!.Where(x => x is ICanBuffAttack).ToList();
            ActivateAbility(abilities);
        }

        private void TryToKillHimHard()
        {
            var abilities = _abilitiesToChoseFrom!.Where(x => x is IDealDamage).ToList();
            ActivateAbility(abilities);
        }

        private void TryToHillYourself()
        {
            var abilities = _abilitiesToChoseFrom!.Where(x => x is ICanHeal || x is ICanLeech).ToList();
            ActivateAbility(abilities);
        }

        private void ChoseRandomAbility()
        {
            var abilities = _abilitiesToChoseFrom!.Where(x => x.Cooldown == 4).ToList();
            ActivateAbility(abilities);
        }

        private void ActivateAbility(List<Ability> abilities)
        {
            var amountAbilities = abilities.Count;
            var ability = abilities[_enemyAI!.Rnd.RandiRange(0, amountAbilities)];
            ability?.BuffAttacks(_enemyAI!.EnemyAttack);
        }

        private void UpdateAbilityCooldown()
        {
            foreach (var item in _abilitiesToChoseFrom)
            {
                item.Cooldown--;
            }
        }
    }
}
