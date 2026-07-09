namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Modifiers.Context;

    /// <summary>
    /// Buff: the target takes <c>reduce</c> less damage from all incoming hits per stack.
    /// Each stack carries its own incoming-damage modifier, so stacks multiply.
    /// </summary>
    public class IncomingDamageReductionEffect(int duration, int maxStacks, float reduce)
        : Effect(id: "Effect_Incoming_Damage_Reduction", duration, maxStacks)
    {
        private IDamageModifier? _modifier;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied) return;

            _modifier = new IncomingDamageReductionContextModifier( reduce);
            Target?.ModifierHandler.Add(_modifier);
        }

        public override void Remove()
        {
            if (_modifier != null) Target?.ModifierHandler.Remove(_modifier);
            base.Remove();
        }

        public override IEffect Copy() => new IncomingDamageReductionEffect(Duration, MaxStacks, reduce);
    }
}
