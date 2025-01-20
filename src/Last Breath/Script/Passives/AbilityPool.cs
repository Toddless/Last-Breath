namespace Playground.Script.Passives
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Passives.Attacks;
    using Playground.Script.Passives.Buffs;

    public class AbilityPool : IDisposable
    {
        private List<IAbility>? _abilities = new();

        protected List<IAbility>? Abilities
        {
            get => _abilities;
            set => _abilities = value;
        }

        public AbilityPool(BaseEnemy enemy)
        {
            SetTargetComponents(enemy);
        }

        private void SetTargetComponents(BaseEnemy enemy)
        {
            if (enemy.EnemyAttack == null || enemy.EnemyHealth == null || enemy.EnemyAttribute == null)
                return;
            Abilities?.Add(new OneShotHeal(enemy.EnemyHealth));
            Abilities?.Add(new Regeneration(enemy.EnemyHealth));
            Abilities?.Add(new BuffAttack(enemy.EnemyAttack));
            Abilities?.Add(new BuffCriticalStrikeChance(enemy.EnemyAttack));
            Abilities?.Add(new BuffCriticalStrikeDamage(enemy.EnemyAttack));
            Abilities?.Add(new VampireStrike(enemy.EnemyAttack));
            Abilities?.Add(new DoubleStrike(enemy.EnemyAttack));
            Abilities?.Add(new JustCreatingNewAbility(enemy.EnemyAttribute));
        }

        public IAbility? GetNewAbilityWithSpecificRarity(GlobalRarity rarity)
        {
            return Abilities?.FirstOrDefault(x => x.Rarity == rarity);
        }

        public List<IAbility>? SelectAbilities(int count)
        {
            if (count > Abilities?.Count || Abilities == null)
            {
                throw new ArgumentException("");
            }
            using (var rnd = new RandomNumberGenerator())
            {
                List<IAbility> result = [];
                while (result.Count != count)
                {
                    var randomNumber = rnd.RandiRange(0, Abilities.Count - 1);
                    var chosenAbility = Abilities[randomNumber];
                    if (!result.Contains(chosenAbility))
                    {
                        result.Add(chosenAbility);
                    }
                }

                return result;
            }
        }

        public List<IAbility> GetAllAbilities()
        {
            List<IAbility> x = [];
            foreach (var ability in Abilities!)
            {
                x.Add(ability);
            }
            return x;
        }

        public void Dispose()
        {
            Abilities?.Clear();
            Abilities = null;
            GC.SuppressFinalize(this);
        }
    }
}
