namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;

    public class SoAsDefaultExecutionStrategy : ISoAExecutionStrategy
    {
        public virtual async Task Execute(SeriesOfAttacks ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            int attacks = CombatRandom.Rolls.RandIntRange(ability.MinAttacks, ability.MaxAttacks);

            foreach (IFightable target in targets)
                await ExecuteOnTarget(target);
            return;

            async Task ExecuteOnTarget(IFightable target)
            {
                var window = new AttackSeriesWindow(ability, owner, field);
                // MaxAttacks caps the ROLL, not the window: the planned hits are guaranteed and
                // reaction extras land on top (IP semantics). Counting extras against the cap
                // silently swallowed the bought attack-count upgrades.
                for (int i = 0; i < attacks; i++)
                {
                    float additionalDamage = ability.Damage + ((owner.Parameters.Damage * ability.WeaponDamageScale) + (owner.Parameters.SpellDamage * ability.SpellDamageScale));
                    var context = new AttackContext(owner, target, owner.Parameters.Damage, CombatRandom.Attacks!, window.Scheduler)
                    {
                        Index = i,
                        TotalCount = attacks, // the rolled series length — "last hit" logic keys off it
                        SourceAbilityId = ability.Id
                    };
                    context.UseCriticalOf(ability);
                    context.UseAccuracyOf(ability);
                    context.AddDamage(DamageType.Physical, additionalDamage);
                    // Pre-attack mutators run BEFORE the attack is scheduled so they shape the roll.
                    ability.AttackModifiers.ApplyAll(context);

                    // An evade past the second hit aborts the rest of the series (unless made unevadable).
                    bool resolved = await window.ResolveAsync(context, processed =>
                        Task.FromResult(processed.Result is not AttackResults.Evaded || !ability.IsEvadable || window.OwnerAttacks < 2));
                    if (!resolved) return;
                }
            }
        }
    }
}
