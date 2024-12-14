namespace Playground.Script.Passives
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Passives.Attacks;

    public class AbilityPool
    {
        private readonly List<Ability> _abilities = [
            new DoubleStrike(),
            new VampireStrike(),
            new BuffAttack(),
            new BuffCriticalStrikeChance(),
            new BuffCriticalStrikeChance()
            ];
        public AbilityPool()
        {
        }

        public List<Ability> SelectAbilities(int count)
        {
            if(count > _abilities.Count)
            {
                throw new ArgumentException("");
            }
            using (var rnd = new RandomNumberGenerator())
            {
                List<Ability> result = [];
                while (result.Count != count)
                {
                    var randomNumber = rnd.RandiRange(0, _abilities.Count - 1);
                    var chosenAbility = _abilities[randomNumber];
                    if (!result.Contains(chosenAbility))
                    {
                        result.Add(chosenAbility);
                    }
                }
                
                return result;
            }
        }
    }
}
