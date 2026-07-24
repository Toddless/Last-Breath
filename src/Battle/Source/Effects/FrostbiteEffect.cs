namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>
    /// "Обморожение": the target takes <c>coldDamageAmp</c> more Cold damage per stack.
    /// Each stack carries its own incoming-damage modifier, so stacks multiply (Fragility pattern).
    /// </summary>
    public class FrostbiteEffect(int duration, int maxStacks, float coldDamageAmp)
        : Effect(id: "Effect_Frostbite", duration, maxStacks)
    {
        public override bool IsHarmful => true;

        private IDamageModifier? _modifier;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return; // a rejected stack must not amplify anything

            _modifier = new DamageTypeTakenContextModifier(Target, DamageType.Cold, coldDamageAmp);
            Target.ModifierHandler.Add(_modifier);
        }

        public override void Remove()
        {
            if (_modifier != null) Target?.ModifierHandler.Remove(_modifier);
            base.Remove();
        }

        public override IEffect Copy() => new FrostbiteEffect(Duration, MaxStacks, coldDamageAmp);
    }
}
