namespace Playground.Script.Passives
{
    using System;
    using System.Collections.Generic;
    using Godot;

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
                // If we do not have a lot of skills and amount is only a little bit lower than count, this part can take too long.
                while (result.Count != amount)
                {
                    var chosenAbility = _abilities[rnd.RandiRange(0, _abilities.Count - 1)];
                    if (!result.Contains(chosenAbility))
                    {
                        result.Add(chosenAbility);
                        _abilities.Remove(chosenAbility);
                    }
                    if (_abilities.Count <= 0)
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
