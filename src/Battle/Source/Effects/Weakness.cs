namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Modifiers.Context;

    /// <summary>
    /// "Бессилие": the target's ability HITS deal <c>value</c> less damage per stack (attacks are the
    /// domain of <see cref="FeeblenessEffect"/>). Each stack carries its own outgoing-damage modifier,
    /// so stacks multiply.
    /// </summary>
    public class Weakness(int duration, int maxStacks, EffectValue value)
        : Effect(id: "Effect_Weakness", duration, maxStacks)
    {
        public override bool IsHarmful => true;

        private IDamageModifier? _modifier;

        /// <summary>What the bearer's hits are left dealing — the authored loss through effectiveness,
        /// inverted after it (never below nothing).</summary>
        public float DamageLeft => Effective(value, EffectValueShape.ShareLost);

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return; // a rejected stack must not weaken anything

            _modifier = new HitDamageDealtContextModifier(Target, DamageLeft);
            Target.ModifierHandler.Add(_modifier);
        }

        public override void Remove()
        {
            if (_modifier != null) Target?.ModifierHandler.Remove(_modifier);
            base.Remove();
        }

        public override IEffect Copy() => new Weakness(Duration, MaxStacks, value);
    }
}
