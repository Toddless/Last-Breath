namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Godot;

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
                    SourceAbilityId = ability.Id
                };

                ability.AttackModifiers.ApplyAll(context);

                if (!context.Schedule()) break;
                // Riders fire for every owner attack processed in the window, extra attacks included.
                await foreach (var processed in scheduler.RunQueue(cts.Token))
                {
                    if (processed.Attacker.InstanceId != owner.InstanceId) continue;
                    await ability.ApplyImpactRiders(processed.ToImpact(field));
                }
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
