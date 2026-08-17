namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Context;
    using Core.Entity;

    public class IpSingleAttackExecutionStrategy : IIpExecutionStrategy
    {
        public async Task Execute(IncreasingPressure ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            foreach (IFightable target in targets)
            {
                float totalDamage = CalculateTotalDamage(ability, owner);
                var window = new AttackSeriesWindow(ability, owner, field);
                var context = new AttackContext(owner, target, totalDamage, CombatRandom.Attacks!, window.Scheduler)
                {
                    SourceAbilityId = ability.Id
                };

                context.UseCriticalOf(ability);
                ability.AttackModifiers.ApplyAll(context);

                if (!await window.ResolveAsync(context)) break;
            }
        }

        private float CalculateTotalDamage(IncreasingPressure ability, IFightable owner)
        {
            float increase = 1f;
            float totalDamage = 0f;
            // Escalation here is unconditional (a single lumped attack can't know per-hit results);
            // the default strategy instead escalates per resolved hit and stops the series on an evade.
            for (int i = 0; i < ability.Attacks; i++)
            {
                totalDamage += ability.PerHitDamage(owner, increase);
                increase += ability.AttackDamageMultiplier;
            }

            return totalDamage;
        }
    }
}
