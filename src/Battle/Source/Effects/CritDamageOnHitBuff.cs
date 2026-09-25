namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Entity.Components.Decorator;
    using Core.Enums;
    using Core.Events;
    using Core.Modifiers;

    /// <summary>
    /// Buff that increases critical damage and additionally raises the bearer's critical chance
    /// by a flat amount after each successful attack.
    /// Nothing builds one: no ability and no augment constructs this buff, so it is a mechanic the
    /// catalog currently has no record for.
    /// </summary>
    public class CritDamageOnHitBuff(
        EffectValue critDamageBonus,
        int duration,
        EffectValue critDamagePerHit,
        int maxStacks = 1,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Crit_Damage_On_Hit_Buff", duration, maxStacks, statusEffect)
    {
        private const string DecoratorId = "Effect_Crit_Dmg_Buff_Decorator";

        private EntityParameterDecorator? _critDamageDecorator;

        public float CritDamageBonus => Effective(critDamageBonus);

        public float CritDamagePerHit => Effective(critDamagePerHit);

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(critDamageBonus)] = CritDamageBonus;
                values[nameof(critDamagePerHit)] = CritDamagePerHit;
                return values;
            }
        }

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return; // a rejected stack must not decorate anything

            // Built here and not in a field initializer: a field runs at construction, before the laying
            // cast's effectiveness is stamped, so the decorator carried the unscaled figure for good.
            _critDamageDecorator = new EntityParameterDecorator(DecoratorId, CritDamageBonus, OperationType.Add, EntityParameter.CriticalDamage, Priority.Weak);
            Target.Parameters.AddModuleDecorator(_critDamageDecorator);
            SubscribeUntilRemoved<AfterAttackEvent>(Target.CombatEvents, OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Target == null) return;
            if (evt.Context.Result != AttackResults.Succeed && !evt.Context.IsCritical) return;

            // Flat increase to crit damage on each successful critical attack
            var modifier = new SimpleModifier(EntityParameter.PhysicalDamage, ModifierValueType.Flat, CritDamagePerHit, $"CC_CritChance_OnHit_{InstanceId}");
            modifier.ApplyTo(Target);
        }

        public override void Remove()
        {
            if (_critDamageDecorator != null) Target?.Parameters.RemoveModuleDecorator(_critDamageDecorator.Id, EntityParameter.CriticalDamage);
            Target?.ParameterModifiers.RemoveModifierBySource($"CC_CritChance_OnHit_{InstanceId}");
            base.Remove();
        }

        public override IEffect Copy() => new CritDamageOnHitBuff(critDamageBonus, Duration, critDamagePerHit, MaxStacks, Status);
    }
}
