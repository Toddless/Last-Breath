namespace Battle.Source.Abilities.HitDelivery
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Entity;
    using Godot;

    /// <summary>
    /// Bounce delivery: N landings on random living enemies, avoiding back-to-back repeats when
    /// more than one enemy is alive. The same enemy may be hit again on later bounces.
    /// </summary>
    public sealed class BouncingHits(int bounces) : IHitSequenceStrategy
    {
        public IReadOnlyList<IFightable> GetHitSequence(IFightable owner, IReadOnlyList<IFightable> targets, IBattleField field)
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();

            List<IFightable> sequence = [];
            IFightable? lastTarget = null;
            for (int i = 0; i < bounces; i++)
            {
                var alive = field.GetEnemies(owner).Where(e => e.IsAlive).ToList();
                var pool = alive.Count > 1 ? alive.Where(e => e != lastTarget).ToList() : alive;
                if (pool.Count == 0) break;

                var target = pool[rnd.RandiRange(0, pool.Count - 1)];
                sequence.Add(target);
                lastTarget = target;
            }

            return sequence;
        }
    }
}
