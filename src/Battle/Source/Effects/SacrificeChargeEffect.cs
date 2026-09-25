namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Events;
    using Core.Modifiers.Context;

    /// <summary>
    /// The Sacrifice charge: the next <c>charges</c> activated abilities deal extra SACRED damage equal to
    /// <c>bonusPercent</c> of their damage. Lives by activations, not turns — the boost attaches on
    /// AbilityActivated and detaches on AbilityExecuted (the cast window). Recasting Sacrifice itself
    /// does not consume a charge. With <c>healPercent</c> &gt; 0 the caster heals for a share of the
    /// damage the boosted cast dealt (pre-mitigation).
    /// </summary>
    public class SacrificeChargeEffect(string sourceAbilityId, int charges, EffectValue bonusPercent, EffectValue healPercent = default)
        : Effect(id: "Effect_Sacrifice_Charge", duration: 0, maxStacks: 1)
    {
        private int _chargesLeft = charges;
        private SacredDamageBonusContextModifier? _modifier;
        private string _boostedCastId = string.Empty;

        /// <summary>Share of the boosted cast's damage added again as sacred.</summary>
        public float BonusPercent => Effective(bonusPercent);

        /// <summary>Share of the damage the boosted cast dealt that comes back as health.</summary>
        public float HealPercent => Effective(healPercent);

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
            otherEffect is SacrificeChargeEffect other && BonusPercent > other.BonusPercent;

        public override IEffect Copy() => new SacrificeChargeEffect(sourceAbilityId, _chargesLeft, bonusPercent, healPercent);

        private void OnAbilityActivated(AbilityActivatedEvent evt)
        {
            if (Target == null || _modifier != null) return;
            if (evt.Ability.Id == sourceAbilityId) return; // recasting Sacrifice refreshes, never consumes

            _modifier = new SacredDamageBonusContextModifier(Target, BonusPercent);
            Target.ModifierHandler.Add(_modifier);
            _boostedCastId = evt.CastId;
        }

        private void OnAbilityExecuted(AbilityExecutedEvent evt)
        {
            if (Target == null || _modifier == null || evt.CastId != _boostedCastId) return;

            float damageSeen = _modifier.DamageSeen;
            DetachModifier();
            if (HealPercent > 0 && damageSeen > 0)
                Target.Heal(new HealContext(Target, Target) { Amount = damageSeen * HealPercent });

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
