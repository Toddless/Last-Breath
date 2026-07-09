namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Godot;

    public class SoAsDefaultExecutionStrategy : ISoAExecutionStrategy
    {
        public virtual async Task Execute(SeriesOfAttacks ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            int attacks = rnd.RandiRange(ability.MinAttacks, ability.MaxAttacks);

            foreach (IFightable target in targets)
                await ExecuteOnTarget(target);
            return;

            async Task ExecuteOnTarget(IFightable target)
            {
                var window = new AttackSeriesWindow(ability, owner, field);
                for (int i = 0; i < attacks && window.OwnerAttacks < ability.MaxAttacks; i++)
                {
                    float additionalDamage = ability.Damage + ((owner.Parameters.Damage * ability.WeaponDamageScale) + (owner.Parameters.SpellDamage * ability.SpellDamageScale));
                    var context = new AttackContext(owner, target, owner.Parameters.Damage, rnd, window.Scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance,
                        AdditionalDamage = additionalDamage,
                        Index = i,
                        TotalCount = ability.MaxAttacks,
                        SourceAbilityId = ability.Id
                    };
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
