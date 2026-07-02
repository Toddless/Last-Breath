namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Godot;
    using Utilities;

    public class IpSingleAttackExecutionStrategy : IIpExecutionStrategy
    {
        public async Task Execute(IncreasingPressure ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            var rnd = new RandomNumberGenerator();
            var cts = new CancellationTokenSource();
            rnd.Randomize();

            foreach (IFightable target in targets)
            {
                float totalDamage = CalculateTotalDamage(ability, owner);
                var scheduler = new AttackContextScheduler();
                var context = new AttackContext(owner, target, totalDamage, rnd, scheduler)
                {
                    RawCriticalDamage = owner.Parameters.CriticalDamage, RawCriticalChance = owner.Parameters.CriticalChance,
                };

                foreach (IAttackModifier attackModifier in ability.AttackModifiers)
                    attackModifier.Apply(context);

                if (!context.Schedule()) break;
                await scheduler.DrainQueue(cts.Token);
            }
        }

        private float CalculateTotalDamage(IncreasingPressure ability, IFightable owner)
        {
            float increase = 1f;
            float totalDamage = 0f;
            for (int i = 0; i < ability.Attacks; i++)
            {
                float additionalDamage = ability.Damage + ((owner.Parameters.Damage * ability.WeaponDamageScale) + (owner.Parameters.SpellDamage * ability.SpellDamageScale));
                float damage = (owner.Parameters.Damage + additionalDamage) * increase;
                totalDamage += damage;
                increase += ability.AttackDamageMultiplier;
            }

            return totalDamage;
        }
    }
}
