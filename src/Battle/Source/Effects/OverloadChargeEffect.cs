namespace Battle.Source.Effects
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events;
    using Core.Modifiers.Context;

    /// <summary>
    /// The Overload charge: the NEXT activated ability deals <c>multiplier</c> (0.5 = +50%) more
    /// damage. Lives by activations, not turns — attaches on AbilityActivated, detaches on
    /// AbilityExecuted (the cast window). Recasting Overload itself refreshes, never consumes.
    /// </summary>
    public class OverloadChargeEffect(string sourceAbilityId, float multiplier)
        : Effect(id: "Effect_Overload_Charge", duration: 0, maxStacks: 1)
    {
        private CastDamageScale? _modifier;
        private string _boostedCastId = string.Empty;

        public float Multiplier => multiplier;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;
            SubscribeUntilRemoved<AbilityActivatedEvent>(Target.CombatEvents, OnAbilityActivated);
            SubscribeUntilRemoved<AbilityExecutedEvent>(Target.CombatEvents, OnAbilityExecuted);
        }

        /// <summary>The charge does not decay with turns; it waits for its activation.</summary>
        public override void TurnEnd()
        {
        }

        public override void Remove()
        {
            DetachModifier();
            base.Remove();
        }

        public override bool IsStronger(IEffect otherEffect) =>
            otherEffect is OverloadChargeEffect other && multiplier > other.Multiplier;

        public override IEffect Copy() => new OverloadChargeEffect(sourceAbilityId, multiplier);

        private void OnAbilityActivated(AbilityActivatedEvent evt)
        {
            if (Target == null || _modifier != null) return;
            if (evt.Ability.Id == sourceAbilityId) return; // recasting Overload refreshes, never consumes

            _modifier = new CastDamageScale(Target, 1 + multiplier);
            Target.ModifierHandler.Add(_modifier);
            _boostedCastId = evt.CastId;
        }

        private void OnAbilityExecuted(AbilityExecutedEvent evt)
        {
            if (_modifier == null || evt.CastId != _boostedCastId) return;
            Remove();
        }

        private void DetachModifier()
        {
            if (_modifier == null || Target == null) return;
            Target.ModifierHandler.Remove(_modifier);
            _modifier = null;
            _boostedCastId = string.Empty;
        }

        /// <summary>Outgoing mutator of the boosted cast: scales every ability damage component.</summary>
        private sealed class CastDamageScale(Core.Entity.IFightable owner, float factor)
            : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Overload_Charge"), IDamageModifier
        {
            public void Apply(IDamageContext context)
            {
                if (!context.Source.IsSame(owner.InstanceId)) return;
                if (context.Cause is not DamageCause.Ability) return;
                foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                    context.Set(type, damage * factor);
            }
        }
    }
}
