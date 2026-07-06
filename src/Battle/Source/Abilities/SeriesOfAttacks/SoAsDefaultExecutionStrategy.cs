namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Godot;

    public class SoAsDefaultExecutionStrategy : ISoAExecutionStrategy
    {
        public virtual async Task Execute(SeriesOfAttacks ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            var cts = new CancellationTokenSource();
            int attacks = rnd.RandiRange(ability.MinAttacks, ability.MaxAttacks);

            foreach (IFightable target in targets)
                await ExecuteOnTarget(target);
            return;

            async Task ExecuteOnTarget(IFightable target)
            {
                var scheduler = new AttackContextScheduler();
                int processedCount = 0;
                for (int i = 0; i < attacks && processedCount < ability.MaxAttacks; i++)
                {
                    float additionalDamage = ability.Damage + ((owner.Parameters.Damage * ability.WeaponDamageScale) + (owner.Parameters.SpellDamage * ability.SpellDamageScale));
                    var context = new AttackContext(owner, target, owner.Parameters.Damage, rnd, scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance,
                        AdditionalDamage = additionalDamage,
                        Index = i,
                        TotalCount = ability.MaxAttacks,
                        SourceAbilityId = ability.Id
                    };
                    // Pre-attack mutators run BEFORE the attack is scheduled so they shape the roll.
                    ability.AttackModifiers.ApplyAll(context);

                    if (!context.Schedule()) break;
                    await foreach (var processed in scheduler.RunQueue(cts.Token))
                    {
                        // Every owner attack processed within the series counts as an ability hit —
                        // including extra attacks spawned by reactions — so impact riders fire for each.
                        if (processed.Attacker.InstanceId == owner.InstanceId)
                        {
                            processedCount++;
                            await ability.ApplyImpactRiders(processed.ToImpact(field));
                        }

                        if (processed.Result is not AttackResults.Evaded || !ability.IsEvadable || processedCount < 2) continue;

                        await cts.CancelAsync();
                        return;
                    }
                }
            }
        }
    }
}
