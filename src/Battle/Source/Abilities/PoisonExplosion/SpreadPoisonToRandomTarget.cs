namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    public class SpreadPoisonToRandomTarget : IPoisonSpreadMode
    {
        public void SpreadPoison(List<DamageOverTurnEffect> originalStacks, IFightable originalTarget, IFightable owner, IBattleField field, string source)
        {
            var poisonStacks = originalTarget.Effects
                .GetBy(e => e.Status == StatusEffects.Poison)
                .OfType<DamageOverTurnEffect>()
                .ToList();

            var newTarget = field.GetRandomEntity(originalTarget);

            foreach (DamageOverTurnEffect stack in poisonStacks)
            {
                var copy = (DamageOverTurnEffect)stack.Copy();
                copy.Apply(new EffectApplyingContext { Caster = owner, Target = newTarget, Source = source, Damage = stack.DamagePerTick });
            }
        }
    }
}
