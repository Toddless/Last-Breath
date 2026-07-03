namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Abilities.Modifiers;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// "Хрупкость": the target takes <c>critDamageAmp</c> more damage from critical hits per stack.
    /// Each stack carries its own incoming-damage modifier, so stacks multiply.
    /// </summary>
    public class FragilityEffect(int duration, int maxStacks, float critDamageAmp)
        : Effect(id: "Effect_Fragility", duration, maxStacks)
    {
        private IDamageModifier? _modifier;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied) return; // a rejected stack must not amplify anything

            _modifier = new CritDamageTakenModifier(Priority.Weak, critDamageAmp);
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
