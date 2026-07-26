namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
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
            rnd.Randomize();

            foreach (IFightable target in targets)
            {
                var window = new AttackSeriesWindow(ability, owner, field);
                float increase = 1f;
                for (int i = 0; i < ability.Attacks; i++)
                {
                    if (!target.IsAlive) break;
                    float additionalDamage = ability.BonusDamage(owner) * increase;
                    float damage = owner.Parameters.Damage * increase;
                    var context = new AttackContext(owner, target, damage, rnd, window.Scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance,
                        RawCriticalDamage = owner.Parameters.CriticalDamage,
                        Index = i,
                        TotalCount = (int)ability.Attacks,
                        SourceAbilityId = ability.Id
                    };
                    context.AddDamage(DamageType.Physical, additionalDamage);

                    ability.AttackModifiers.ApplyAll(context);

                    if (!await window.ResolveAsync(context)) break;

                    // Escalation is per RESOLVED hit: a miss ends the pressure build-up.
                    if (context.Result is not AttackResults.Succeed) break;
                    increase += ability.AttackDamageMultiplier;
                }
            }
        }
    }
}
