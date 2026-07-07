namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Godot;

    public class IpDefaultExecutionStrategy : IIpExecutionStrategy
    {
        public virtual async Task Execute(IncreasingPressure ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            var rnd = new RandomNumberGenerator();
            var cts = new CancellationTokenSource();
            rnd.Randomize();

            foreach (IFightable target in targets)
            {
                var scheduler = new AttackContextScheduler();
                float increase = 1f;
                for (int i = 0; i < ability.Attacks; i++)
                {
                    if (!target.IsAlive) break;
                    float additionalDamage = ability.Damage + ((owner.Parameters.Damage * ability.WeaponDamageScale) + (owner.Parameters.SpellDamage * ability.SpellDamageScale));
                    additionalDamage *= increase;
                    float damage = owner.Parameters.Damage * increase;
                    var context = new AttackContext(owner, target, damage, rnd, scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance,
                        RawCriticalDamage = owner.Parameters.CriticalDamage,
                        AdditionalDamage = additionalDamage,
                        Index = i,
                        TotalCount = (int)ability.Attacks,
                        SourceAbilityId = ability.Id
                    };

                    ability.AttackModifiers.ApplyAll(context);

                    if (!context.Schedule()) break;
                    // Every owner attack processed within the series counts as an ability hit —
                    // including extra attacks spawned by reactions — so impact riders fire for each.
                    await foreach (var processed in scheduler.RunQueue(cts.Token))
                    {
                        if (processed.Attacker.InstanceId != owner.InstanceId) continue;
                        await ability.ApplyImpactRiders(processed.ToImpact(field));
                    }

                    if (context.Result is AttackResults.Succeed) increase += ability.AttackDamageMultiplier;
                }
            }
        }
    }
}
