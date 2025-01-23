namespace Playground.Script.Passives
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Passives.Attacks;

    public class AbilityPool : IDisposable
    {
        private List<IAbility>? _abilities = new();

        public AbilityPool(BaseEnemy enemy)
        {
            SetTargetComponents(enemy);
        }

        private void SetTargetComponents(BaseEnemy enemy)
        {
            if (enemy.AttackComponent == null || enemy.HealthComponent == null || enemy.EnemyAttribute == null)
                return;
            _abilities?.Add(new OneShotHeal());
            _abilities?.Add(new Regeneration());
            _abilities?.Add(new BuffAttack());
            _abilities?.Add(new BuffCriticalStrikeChance());
            _abilities?.Add(new BuffCriticalStrikeDamage());
            _abilities?.Add(new VampireStrike());
            _abilities?.Add(new DoubleStrike());
        }

       // public IAbility? GetNewAbilityWithSpecificRarity(GlobalRarity rarity) => _abilities?.FirstOrDefault(x => x.Rarity == rarity);

        public List<IAbility>? SelectAbilities(int amount)
        {
            if (amount > _abilities?.Count || _abilities == null)
            {
                throw new ArgumentException("");
            }
            using (var rnd = new RandomNumberGenerator())
            {
                List<IAbility> result = [];
                while (result.Count != amount)
                {
                    // If we do not have a lot of skills, and amount is only a little bit lower than count, this part can take too long.
                    var chosenAbility = _abilities[rnd.RandiRange(0, _abilities.Count - 1)];
                    if (!result.Contains(chosenAbility))
                    {
                        result.Add(chosenAbility);
                        _abilities.Remove(chosenAbility);
                    }
                    if(_abilities.Count <= 0)
                        break;
                }

                return result;
            }
        }

        public void Dispose()
        {
            _abilities?.Clear();
            _abilities = null;
            GC.SuppressFinalize(this);
        }
    }
}
