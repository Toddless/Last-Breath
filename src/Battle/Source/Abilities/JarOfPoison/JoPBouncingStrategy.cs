namespace Battle.Source.Abilities.JarOfPoison
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Godot;

    /// <summary>
    /// L3 upgrade strategy: the jar bounces 5 times between random enemies,
    /// applying poison to each target it lands on.
    /// </summary>
    public class JoPBouncingStrategy(int bounces = 5) : JoPDefaultExecutionStrategy
    {
        public override async Task Execute(JarOfPoison ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();

            var enemies = field.GetEnemies(owner).Where(e => e.IsAlive).ToList();

            if (enemies.Count == 0) return;

            IFightable? lastTarget = null;
            for (int i = 0; i < bounces; i++)
            {
                // Prefer targets other than the last to avoid back-to-back hits on same target
                var pool = enemies.Count > 1
                    ? enemies.Where(e => e != lastTarget && e.IsAlive).ToList()
                    : enemies.Where(e => e.IsAlive).ToList();

                if (pool.Count == 0) break;

                IFightable target = pool[rnd.RandiRange(0, pool.Count - 1)];
                await ApplyToTarget(ability, owner, target);
                lastTarget = target;
            }
        }
    }
}
