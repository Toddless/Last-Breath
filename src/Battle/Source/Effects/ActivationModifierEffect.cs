namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Base for effects whose whole payload is a cast mutator: while the effect lasts, every ability
    /// activation of the bearer goes through the descendant's <see cref="IAbilityActivationModifier"/>
    /// (seals, curses, casting-cost buffs). Each stack carries its own modifier instance.
    /// </summary>
    public abstract class ActivationModifierEffect(
        string id,
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, maxStacks, statusEffect)
    {
        private IAbilityActivationModifier? _modifier;

        /// <summary>The mutator this stack installs. A method rather than a delegate handed to the base
        /// constructor, because a figure of applied content has to be read through <c>Effective</c> and
        /// nothing in a base-constructor argument can reach it — which is how this whole family came to
        /// hand its numbers over raw.</summary>
        protected abstract IAbilityActivationModifier CreateModifier();

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return; // a rejected stack must not mutate casts

            // Built after the application, so the effectiveness of the laying cast is already stamped.
            _modifier = CreateModifier();
            Target.ModifierHandler.Add(_modifier);
        }

        public override void Remove()
        {
            if (_modifier != null) Target?.ModifierHandler.Remove(_modifier);
            base.Remove();
        }
    }
}
