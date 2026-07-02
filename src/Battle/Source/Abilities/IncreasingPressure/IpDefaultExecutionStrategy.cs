namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Godot;
    using Utilities;

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
                        TotalCount = (int)ability.Attacks
                    };

                    foreach (var modifier in ability.AttackModifiers)
                        modifier.Apply(context);

                    if (!context.Schedule()) break;
                    await scheduler.DrainQueue(cts.Token);

                    if (context.Result is AttackResults.Succeed) increase += ability.AttackDamageMultiplier;
                }
            }
        }
    }
}
