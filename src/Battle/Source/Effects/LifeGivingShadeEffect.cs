namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events;
    using Core.Modifiers;

    public class LifeGivingShadeEffect : Effect
    {
        private readonly IModifierInstance _modifier;

        public LifeGivingShadeEffect(float lifeToRecover,
            int duration,
            int activationAmount,
            StatusEffects statusEffect = StatusEffects.Regeneration) : base(id: "Effect_Life_Giving_Shade", duration, maxStacks: 1, statusEffect)
        {
            LifeToRecover = lifeToRecover;
            Activations = activationAmount;
            _modifier = new SimpleModifier(EntityParameter.Evade, ModifierValueType.Increase, 0.15f, Id);
        }

        public float LifeToRecover { get; }
        public int Activations { get; private set; }

        public override async Task Apply(EffectApplyingContext context)
        {
            // Whom the shade landed on is settled by the base and nowhere earlier: it is what assigns
            // Target, so a guard on Target ahead of the call refuses every fresh instance and the
            // effect never lands at all. What the base leaves null it left null on purpose — a resisted
            // application — and the two lines below skip themselves for it.
            await base.Apply(context);
            if (Target == null) return;

            _modifier.Copy().ApplyTo(Target);
            Target.CombatEvents.Subscribe<AttackEvadedEvent>(OnAttackEvaded);
        }

        private void OnAttackEvaded(AttackEvadedEvent obj)
        {
            // TODO: Update activation on same effect applying??
            Target?.Heal(new HealContext(Target, Target) { Amount = LifeToRecover });
            Activations--;
            if (Activations == 0) Remove();
        }

        public override void Remove()
        {
            Target?.CombatEvents.Unsubscribe<AttackEvadedEvent>(OnAttackEvaded);
            Target?.ParameterModifiers.RemoveModifierBySource(Id);
            base.Remove();
        }

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values["Evade"] = _modifier.Value * 100f;
                values["Stacks"] = Activations;
                values["Regeneration"] = LifeToRecover;
                return values;
            }
        }

        public override IEffect Copy() => new LifeGivingShadeEffect(LifeToRecover, Duration, Activations, Status);
    }
}
