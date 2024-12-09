namespace Playground.Script.Passives
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Passives.Attacks;

    public abstract class AbilityPool
    {
        private readonly List<Ability> _abilities = [
            new DoubleStrike(),
            new VampireStrike(),
            new BuffAttack(),
            new BuffCriticalStrikeChance(),
            new BuffCriticalStrikeDamage(),
            ];

        public Ability GetRandomAbility()
        {
            using (var rnd = new RandomNumberGenerator())
            {
                return _abilities[rnd.RandiRange(0, _abilities.Count - 1)];
            }
        }
    }
}
