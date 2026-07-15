namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Entity.Components.Decorator;
    using Core.Enums;
    using Core.Events;
    using Core.Modifiers;

    /// <summary>
    /// Buff that increases critical damage and additionally raises the bearer's critical chance
    /// by a flat amount after each successful attack.
    /// Used by CriticalCalculation L3 upgrade.
    /// </summary>
    public class CritDamageOnHitBuff(
        float critDamageBonus,
        int duration,
        float critDamagePerHit,
        int maxStacks = 1,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Crit_Damage_On_Hit_Buff", duration, maxStacks, statusEffect)
    {
        private readonly EntityParameterDecorator _critDamageDecorator = new("Effect_Crit_Dmg_Buff_Decorator",
            critDamageBonus,
            OperationType.Add,
            EntityParameter.CriticalDamage,
            Priority.Weak);

        public override async Task Apply(EffectApplyingContext context)
        {
            context.Target.Parameters.AddModuleDecorator(_critDamageDecorator);
            await base.Apply(context);
            if (Target == null) return;
            SubscribeUntilRemoved<AfterAttackEvent>(Target.CombatEvents, OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Target == null) return;
            if (evt.Context.Result != AttackResults.Succeed && !evt.Context.IsCritical) return;

            // Flat increase to crit damage on each successful critical attack
            var modifier = new SimpleModifier(EntityParameter.Damage, ModifierValueType.Flat, critDamagePerHit, $"CC_CritChance_OnHit_{InstanceId}");
            modifier.ApplyTo(Target);
        }

        public override void Remove()
        {
            Target?.Parameters.RemoveModuleDecorator(_critDamageDecorator.Id, EntityParameter.CriticalDamage);
            Target?.ParameterModifiers.RemoveModifierBySource($"CC_CritChance_OnHit_{InstanceId}");
            base.Remove();
        }

        public override IEffect Copy() => new CritDamageOnHitBuff(critDamageBonus, Duration, critDamagePerHit, MaxStacks, Status);
    }
}
