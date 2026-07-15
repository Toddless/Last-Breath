namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Events;
    using Core.Modifiers.Context;

    /// <summary>
    /// The Sacrifice charge: the next <c>charges</c> activated abilities deal extra PURE damage equal to
    /// <c>bonusPercent</c> of their damage. Lives by activations, not turns — the boost attaches on
    /// AbilityActivated and detaches on AbilityExecuted (the cast window). Recasting Sacrifice itself
    /// does not consume a charge. With <c>healPercent</c> &gt; 0 the caster heals for a share of the
    /// damage the boosted cast dealt (pre-mitigation).
    /// </summary>
    public class SacrificeChargeEffect(string sourceAbilityId, int charges, float bonusPercent, float healPercent = 0)
        : Effect(id: "Effect_Sacrifice_Charge", duration: 0, maxStacks: 1)
    {
        private int _chargesLeft = charges;
        private PureDamageBonusContextModifier? _modifier;
        private string _boostedCastId = string.Empty;

        public float BonusPercent => bonusPercent;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;
            SubscribeUntilRemoved<AbilityActivatedEvent>(Target.CombatEvents, OnAbilityActivated);
            SubscribeUntilRemoved<AbilityExecutedEvent>(Target.CombatEvents, OnAbilityExecuted);
        }

        /// <summary>The charge does not decay with turns; it waits for its activations.</summary>
        public override void TurnEnd()
        {
        }

        public override void Remove()
        {
            DetachModifier();
            base.Remove();
        }

        public override bool IsStronger(IEffect otherEffect) =>
            otherEffect is SacrificeChargeEffect other && bonusPercent > other.BonusPercent;

        public override IEffect Copy() => new SacrificeChargeEffect(sourceAbilityId, charges, bonusPercent, healPercent);

        private void OnAbilityActivated(AbilityActivatedEvent evt)
        {
            if (Target == null || _modifier != null) return;
            if (evt.Ability.Id == sourceAbilityId) return; // recasting Sacrifice refreshes, never consumes

            _modifier = new PureDamageBonusContextModifier(Target, bonusPercent);
            Target.ModifierHandler.Add(_modifier);
            _boostedCastId = evt.CastId;
        }

        private void OnAbilityExecuted(AbilityExecutedEvent evt)
        {
            if (Target == null || _modifier == null || evt.CastId != _boostedCastId) return;

            float damageSeen = _modifier.DamageSeen;
            DetachModifier();
            if (healPercent > 0 && damageSeen > 0)
                Target.Heal(new HealContext(Target, Target) { Amount = damageSeen * healPercent });

            _chargesLeft--;
            if (_chargesLeft <= 0) Remove();
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
