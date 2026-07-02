namespace Battle.Source.Abilities.JarOfPoison
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Effects;

    public class JoPDefaultExecutionStrategy : IJoPExecutionStrategy
    {
        public virtual async Task Execute(JarOfPoison ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            foreach (IFightable target in targets)
                await ApplyToTarget(ability, owner, target);
        }

        protected async Task ApplyToTarget(JarOfPoison ability, IFightable owner, IFightable target)
        {
            float damage = ability.Damage + (owner.Parameters.Damage * ability.WeaponDamageScale) + (owner.Parameters.SpellDamage * ability.SpellDamageScale);

            var context = new EffectApplyingContext { Caster = owner, Target = target, Source = ability.InstanceId, Damage = damage };
            var poison = new DamageOverTurnEffect(ability.PoisonDuration, StatusEffects.Poison);
            await poison.Apply(context);
        }
    }
}
