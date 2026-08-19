namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Modifiers.Context;

    /// <summary>
    /// "Хрупкость": the target takes <c>critDamageAmp</c> more damage from critical hits per stack.
    /// Each stack carries its own incoming-damage modifier, so stacks multiply.
    /// </summary>
    public class FragilityEffect(int duration, int maxStacks, EffectValue critDamageAmp)
        : Effect(id: "Effect_Fragility", duration, maxStacks)
    {
        public override bool IsHarmful => true;

        private IDamageModifier? _modifier;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied) return; // a rejected stack must not amplify anything

            // The modifier adds one to the share itself, so what it wants is the share and not the factor.
            _modifier = new CritDamageTakenContextModifier(Effective(critDamageAmp));
            Target?.ModifierHandler.Add(_modifier);
        }

        public override void Remove()
        {
            if (_modifier != null) Target?.ModifierHandler.Remove(_modifier);
            base.Remove();
        }

        public override IEffect Copy() => new FragilityEffect(Duration, MaxStacks, critDamageAmp);
    }
}
