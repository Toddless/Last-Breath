namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Effects;

    public class SpreadPoisonToRandomTarget : IPoisonSpreadMode
    {
        public IReadOnlyList<IFightable> SpreadPoison(
            List<DamageOverTurnEffect> originalStacks, IFightable originalTarget, IFightable owner, IBattleField field, string source)
        {
            // The arena answers GetRandomEntity with ANY living fighter and ignores what it is handed:
            // the caster, his allies and the target that has just exploded were all fair game for the
            // spread. The pick is made here instead, off the same list every other random-victim delivery
            // picks from — and now that the spread reports an impact, the choice decides who carries a
            // rider's payload, not merely who catches a poison stack.
            var candidates = field.GetEnemies(owner)
                .Where(enemy => enemy.IsAlive && !enemy.IsSame(originalTarget.InstanceId))
                .ToList();
            if (candidates.Count == 0) return [];

            IFightable newTarget = candidates[CombatRandom.Rolls.RandIntRange(0, candidates.Count - 1)];

            // The stacks as they were handed over, not what the victim carries now: re-reading his
            // effects here would sweep up whatever the hit riders laid on him a moment ago, so a poison
            // applier seated on the explosion would silently make its own spread bigger.
            foreach (DamageOverTurnEffect stack in originalStacks)
            {
                var copy = (DamageOverTurnEffect)stack.Copy();
                copy.Apply(new EffectApplyingContext { Caster = owner, Target = newTarget, Source = source, Damage = stack.DamagePerTick });
            }

            return [newTarget];
        }
    }
}
