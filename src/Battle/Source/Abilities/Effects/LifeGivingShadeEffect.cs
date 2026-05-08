namespace Battle.Source.Abilities.Effects
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;
    using Utilities;

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
            if (Target == null) return;
            await base.Apply(context);
            var copy = _modifier.Copy();
            copy.ApplyTo(Target);
            Target?.CombatEvents.Subscribe<AttackEvadedEvent>(OnAttackEvaded);
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

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, _modifier.Value, Activations, LifeToRecover);

        public override IEffect Copy() => new LifeGivingShadeEffect(LifeToRecover, Duration, Activations, Status);
    }
}
