namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Effects;

    public class SpreadPoisonToAll : IPoisonSpreadMode
    {
        public void SpreadPoison(List<DamageOverTurnEffect> originalStacks, IEntity originalTarget, IEntity owner, IBattleField field, string source)
        {
            var enemies = field.GetEnemies(owner).Where(e => e.IsAlive && e != originalTarget).ToList();
            if (enemies.Count == 0) return;

            foreach (IEntity enemy in enemies)
            {
                foreach (var stack in originalStacks)
                {
                    var clone = (DamageOverTurnEffect)stack.Copy();
                    clone.Apply(new EffectApplyingContext { Caster = owner, Target = enemy, Source = source, Damage = stack.DamagePerTick });
                }
            }
        }
    }
}
