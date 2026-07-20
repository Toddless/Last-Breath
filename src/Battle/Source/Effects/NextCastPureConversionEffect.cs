namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events;
    using Core.Modifiers.Context;

    /// <summary>
    /// The Overload L3 upgrade payload: the NEXT activated ability deals <c>fraction</c> (0.3 = 30%)
    /// of its damage as Pure (converted, not added). Same cast-window mechanics as the other
    /// "next ability" charges; recasting the source ability never consumes it.
    /// </summary>
    public class NextCastPureConversionEffect(string sourceAbilityId, float fraction)
        : Effect(id: "Effect_Pure_Conversion_Charge", duration: 0, maxStacks: 1)
    {
        private IDamageModifier? _modifier;
        private string _boostedCastId = string.Empty;

        public float Fraction => fraction;

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
            otherEffect is NextCastPureConversionEffect other && fraction > other.Fraction;

        public override IEffect Copy() => new NextCastPureConversionEffect(sourceAbilityId, fraction);

        private void OnAbilityActivated(AbilityActivatedEvent evt)
        {
            if (Target == null || _modifier != null) return;
            if (evt.Ability.Id == sourceAbilityId) return;

            _modifier = new DamageConversionContextModifier(Target, fraction, DamageCause.Ability);
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
    }
}
