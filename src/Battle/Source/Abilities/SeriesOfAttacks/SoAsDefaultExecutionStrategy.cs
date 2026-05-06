namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Godot;
    using Core.Enums;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using Core.Interfaces.Battle;

    public class SoAsDefaultExecutionStrategy : ISoAExecutionStrategy
    {
        public virtual async Task Execute(SeriesOfAttacks ability, IEntity owner, List<IEntity> targets, IBattleField field)
        {
            // условно
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            var cts = new CancellationTokenSource();
            int attacks = rnd.RandiRange(ability.MinAttacks, ability.MaxAttacks);

            foreach (IEntity target in targets)
                await ExecuteOnTarget(target);
            return;

            async Task ExecuteOnTarget(IEntity target)
            {
                var scheduler = new AttackContextScheduler();
                int processedCount = 0;
                for (int i = 0; i < attacks && processedCount < ability.MaxAttacks; i++)
                {
                    float additionalDamage = ability.Damage + ((owner.Parameters.Damage * ability.WeaponDamageScale) + (owner.Parameters.SpellDamage * ability.SpellDamageScale));
                    var context = new AttackContext(owner, target, owner.Parameters.Damage, rnd, scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance, AdditionalDamage = additionalDamage
                    };

                    if (!context.Schedule()) break;
                    await foreach (var processed in scheduler.RunQueue(cts.Token))
                    {
                        if (processed.Attacker.InstanceId == owner.InstanceId) processedCount++;
                        if (processed.Result is AttackResults.Evaded && ability.IsEvadable && processedCount >= 2)
                        {
                            await cts.CancelAsync();
                            return;
                        }
                    }
                }
            }
        }
    }
}
