namespace Battle.Source.Abilities.JarOfPoison
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    /// <summary>
    /// L3 upgrade strategy: applies poison to ALL enemies on the battlefield,
    /// not just the selected target. Falls back to passed targets if group is unavailable.
    /// </summary>
    public class JoPAllTargetsStrategy : JoPDefaultExecutionStrategy
    {
        public override async Task Execute(JarOfPoison ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            foreach (IFightable target in field.GetEnemies(owner))
            {
                if (!target.IsAlive) continue;
                await ApplyToTarget(ability, owner, target);
            }
        }
    }
}
