namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Abilities.Modifiers;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;

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

            _modifier = new IncomingDamageReductionModifier(Priority.Weak, reduce);
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
