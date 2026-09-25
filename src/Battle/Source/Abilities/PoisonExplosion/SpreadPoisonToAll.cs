namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Effects;

    public class SpreadPoisonToAll : IPoisonSpreadMode
    {
        public IReadOnlyList<IFightable> SpreadPoison(
            List<DamageOverTurnEffect> originalStacks, IFightable originalTarget, IFightable owner, IBattleField field, string source, AbilityTrace trace)
        {
            var enemies = field.GetEnemies(owner).Where(e => e.IsAlive && e != originalTarget).ToList();
            if (enemies.Count == 0) return [];

            foreach (IFightable enemy in enemies)
            {
                foreach (var stack in originalStacks)
                {
                    var clone = (DamageOverTurnEffect)stack.Copy();
                    clone.Apply(new EffectApplyingContext { Caster = owner, Target = enemy, Source = source, Trace = trace });
                }
            }

            return enemies;
        }
    }
}
