namespace Core.Modifiers.Context
{
    using System;
    using Battle;
    using Core.Context;
    using Entity;
    using Enums;

    internal interface IContextModifierBinding
    {
        void Attach(IFightable owner);
        void Detach(IFightable owner);
    }

    /// <summary>Maps a data-level context line to the concrete pipeline modifier it stands for.
    /// Whole-number knobs (durations, stacks) read the floored value.</summary>
    internal static class ContextModifierBindings
    {
        public static IContextModifierBinding Create(ContextModifierEntry entry) => entry.Parameter switch
        {
            ContextParameter.HealingEfficiency => new HealBinding(new HealingBonusContextModifier(() => entry.Value)),
            ContextParameter.BleedDuration => new EffectApplicationBinding(new EffectDurationBonusContextModifier(StatusEffects.Bleed, () => entry.WholeValue)),
            ContextParameter.BurningStacks => new EffectApplicationBinding(new BonusEffectStacksContextModifier(StatusEffects.Burning, () => entry.WholeValue)),
            ContextParameter.BurningDamage => new EffectApplicationBinding(new DotDamageBonusContextModifier(StatusEffects.Burning, () => entry.Value)),
            ContextParameter.HealthOnHit => new AttackModifierBinding(new HealthOnHitContextModifier(() => entry.WholeValue)),
            ContextParameter.ManaOnHit => new AttackModifierBinding(new ManaOnHitContextModifier(() => entry.WholeValue)),
            _ => throw new NotSupportedException($"No binding for context parameter '{entry.Parameter}'")
        };

        private sealed class HealBinding(IHealModifier modifier) : IContextModifierBinding
        {
            public void Attach(IFightable owner) => owner.ModifierHandler.Add(modifier);
            public void Detach(IFightable owner) => owner.ModifierHandler.Remove(modifier);
        }

        private sealed class EffectApplicationBinding(IEffectApplicationModifier modifier) : IContextModifierBinding
        {
            public void Attach(IFightable owner) => owner.ModifierHandler.Add(modifier);
            public void Detach(IFightable owner) => owner.ModifierHandler.Remove(modifier);
        }

        private sealed class AttackModifierBinding(IAttackModifier modifier) : IContextModifierBinding
        {
            public void Attach(IFightable owner) => owner.ModifierHandler.Add(modifier);

            public void Detach(IFightable owner) => owner.ModifierHandler.Remove(modifier);
        }
    }
}
