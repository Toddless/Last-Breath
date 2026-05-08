namespace Battle.Source.Abilities.Effects
{
    using System.Threading.Tasks;
    using Battle.Source.Decorators;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components.Decorator;
    using Core.Modifiers;

    /// <summary>
    /// Buff that increases critical damage and additionally raises the bearer's critical chance
    /// by a flat amount after each successful attack.
    /// Used by CriticalCalculation L3 upgrade.
    /// </summary>
    public class CritDamageOnHitBuff(
        float critDamageBonus,
        int duration,
        float critChancePerHit,
        int maxStacks = 1,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Crit_Damage_On_Hit_Buff", duration, maxStacks, statusEffect)
    {
        private readonly ChangeValueDecorator _critDamageDecorator = new(
            EntityParameter.CriticalDamage,
            Priority.Weak,
            "Effect_Crit_Dmg_Buff_Decorator",
            1f + critDamageBonus);

        public override async Task Apply(EffectApplyingContext context)
        {
            context.Target.Parameters.AddModuleDecorator(_critDamageDecorator);
           await base.Apply(context);
        }

        public override void AfterAttack(IAttackContext context)
        {
            if (Target == null) return;
            if (context.Result != AttackResults.Succeed) return;

            // Flat increase to crit chance on each successful attack
            var modifier = new SimpleModifier(EntityParameter.CriticalChance, ModifierValueType.Flat, critChancePerHit, $"CC_CritChance_OnHit_{InstanceId}");
            modifier.ApplyTo(Target);
            // Note: this modifier persists for the remainder of the fight (intentional for stacking).
            // Add Remove tracking if needed.
        }

        public override void Remove()
        {
            Target?.Parameters.RemoveModuleDecorator(_critDamageDecorator.Id, EntityParameter.CriticalDamage);
            base.Remove();
        }

        public override IEffect Copy() =>
            new CritDamageOnHitBuff(critDamageBonus, Duration, critChancePerHit, MaxStacks, Status);
    }
}
