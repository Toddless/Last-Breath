namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>
    /// Deep Freeze L3 payload: the target's NEXT activated ability starts with +<c>amount</c>
    /// cooldown. One-shot — the mutator spends itself on the first activation it touches; the
    /// effect itself expires by duration if the target never casts.
    /// </summary>
    public class NextAbilityCooldownEffect(int duration, float amount)
        : Effect(id: "Effect_Next_Ability_Cooldown", duration, maxStacks: 1)
    {
        private OneShotCooldownIncrease? _modifier;

        public override bool IsHarmful => true;

        public float Amount => amount;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;

            _modifier = new OneShotCooldownIncrease(amount);
            Target.ModifierHandler.Add(_modifier);
        }

        public override void Remove()
        {
            if (_modifier != null) Target?.ModifierHandler.Remove(_modifier);
            base.Remove();
        }

        public override bool IsStronger(IEffect otherEffect) =>
            otherEffect is NextAbilityCooldownEffect other && amount > other.Amount;

        public override IEffect Copy() => new NextAbilityCooldownEffect(Duration, amount);

        /// <summary>Spends itself on the first activation; removing mid-pipeline would mutate the
        /// modifier list while it is iterated, so the spent mutator simply goes inert.</summary>
        private sealed class OneShotCooldownIncrease(float amount)
            : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Next_Ability_Cooldown"), IAbilityActivationModifier
        {
            private bool _spent;

            public void Apply(IAbilityActivationContext context)
            {
                if (_spent || context.IsPreview) return;
                _spent = true;
                context.Cooldown += amount;
            }
        }
    }
}
