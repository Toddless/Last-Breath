namespace Battle.Source.Abilities.JarOfPoison
{
    using Effects;
    using Core.Enums;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;

    public class JoPDefaultExecutionStrategy : IJoPExecutionStrategy
    {
        public virtual async Task Execute(JarOfPoison ability, IEntity owner, List<IEntity> targets, IBattleField field)
        {
            foreach (IEntity target in targets)
                await ApplyToTarget(ability, owner, target);
        }

        protected async Task ApplyToTarget(JarOfPoison ability, IEntity owner, IEntity target)
        {
            float damage = ability.Damage
                           + (owner.Parameters.Damage * ability.WeaponDamageScale)
                           + (owner.Parameters.SpellDamage * ability.SpellDamageScale);

            var context = new EffectApplyingContext { Caster = owner, Target = target, Source = ability.InstanceId, Damage = damage };
            var poison = new DamageOverTurnEffect(ability.PoisonDuration, 999, StatusEffects.Poison);
            await poison.Apply(context);
        }
    }
}
