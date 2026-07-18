namespace Battle.Source.Effects
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Base for effects whose whole payload is a cast mutator: while the effect lasts, every ability
    /// activation of the bearer goes through the given <see cref="IAbilityActivationModifier"/>
    /// (seals, curses, casting-cost buffs). Each stack carries its own modifier instance.
    /// </summary>
    public abstract class ActivationModifierEffect(
        string id,
        int duration,
        int maxStacks,
        Func<IAbilityActivationModifier> modifierFactory,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, maxStacks, statusEffect)
    {
        private IAbilityActivationModifier? _modifier;

        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return; // a rejected stack must not mutate casts

            _modifier = modifierFactory();
            Target.ModifierHandler.Add(_modifier);
        }

        public override void Remove()
        {
            if (_modifier != null) Target?.ModifierHandler.Remove(_modifier);
            base.Remove();
        }
    }
}
