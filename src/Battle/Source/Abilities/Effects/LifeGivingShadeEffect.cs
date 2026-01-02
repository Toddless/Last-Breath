namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces;
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
            _modifier = new ModifierInstance(EntityParameter.Evade, ModifierType.Increase, 0.15f, Id);
        }

        public float LifeToRecover { get; }
        public int Activations { get; private set; }

        public override void Apply(EffectApplyingContext context)
        {
            base.Apply(context);
            Owner?.Modifiers.AddPermanentModifier(_modifier.Copy());
            Owner?.CombatEvents.Subscribe<AttackEvadedEvent>(OnAttackEvaded);
        }

        private void OnAttackEvaded(AttackEvadedEvent obj)
        {
            // TODO: Update activation on same effect applying??
            Owner?.Heal(LifeToRecover);
            Activations--;
            if (Activations == 0) Remove();
        }

        public override void Remove()
        {
            Owner?.CombatEvents.Unsubscribe<AttackEvadedEvent>(OnAttackEvaded);
            Owner?.Modifiers.RemovePermanentModifierBySource(Id);
            base.Remove();
        }

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, _modifier.Value, Activations, LifeToRecover);

        public override IEffect Clone() => new LifeGivingShadeEffect(LifeToRecover, Duration, Activations, Status);
    }
}
