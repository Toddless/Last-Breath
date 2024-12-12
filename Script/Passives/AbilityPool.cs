namespace Playground.Script.Passives
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Passives.Attacks;

    public class AbilityPool
    {
        public AbilityPool()
        {
        }

        public List<Ability> SelectAbilities(List<Ability> abilities, int count)
        {
            if(count > abilities.Count)
            {
                throw new ArgumentException("");
            }
            using (var rnd = new RandomNumberGenerator())
            {
                var randomNumber = rnd.RandiRange(0, abilities.Count - 1);
                List<Ability> result = [];
                while (result.Count != count)
                {
                    var chosenAbility = abilities[randomNumber];
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
