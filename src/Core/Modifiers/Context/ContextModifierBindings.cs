namespace Core.Modifiers.Context
{
    using System;
    using Battle;
    using Battle.Abilities;
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
            ContextParameter.BleedDamage => new EffectApplicationBinding(new DotDamageBonusContextModifier(StatusEffects.Bleed, () => entry.Value)),
            ContextParameter.BurningStacks => new EffectApplicationBinding(new BonusEffectStacksContextModifier(StatusEffects.Burning, () => entry.WholeValue)),
            ContextParameter.BurningDamage => new EffectApplicationBinding(new DotDamageBonusContextModifier(StatusEffects.Burning, () => entry.Value)),
            ContextParameter.PoisonDamage => new EffectApplicationBinding(new DotDamageBonusContextModifier(StatusEffects.Poison, () => entry.Value)),
            ContextParameter.HealthOnHit => new AttackModifierBinding(new HealthOnHitContextModifier(() => entry.WholeValue)),
            ContextParameter.ManaOnHit => new AttackModifierBinding(new ManaOnHitContextModifier(() => entry.WholeValue)),

            // Mythic marks. Damage knobs gate on the owner, so their modifier is built at attach time.
            ContextParameter.PhysicalToFire => ElementalConversion(entry, DamageType.Fire),
            ContextParameter.PhysicalToCold => ElementalConversion(entry, DamageType.Cold),
            ContextParameter.PhysicalToLightning => ElementalConversion(entry, DamageType.Lightning),
            ContextParameter.AttacksIgnoreResistances =>
                new DamageBinding(owner => new IgnoreResistancesContextModifier(owner, DamageCause.Attack)),
            ContextParameter.AddedFireDamage => AddedElemental(entry, DamageType.Fire),
            ContextParameter.AddedColdDamage => AddedElemental(entry, DamageType.Cold),
            ContextParameter.AddedLightningDamage => AddedElemental(entry, DamageType.Lightning),
            ContextParameter.AttackPureConversion =>
                new DamageBinding(owner => new DamageConversionContextModifier(owner, () => entry.Value, DamageCause.Attack)),
            ContextParameter.CooldownResetChance =>
                new ActivationBinding(new ChanceCooldownResetActivationContextModifier(() => entry.Value)),
            ContextParameter.FreeCastChance =>
                new ActivationBinding(new ChanceFreeCastActivationContextModifier(() => entry.Value)),
            ContextParameter.EffectDurationScale =>
                new EffectApplicationBinding(new EffectDurationScaleContextModifier(() => entry.Value)),
            ContextParameter.DamageTakenReductionFromAttack => DamageTakenReduction(entry, DamageCause.Attack),
            ContextParameter.DamageTakenReductionFromAbility => DamageTakenReduction(entry, DamageCause.Ability),
            ContextParameter.DamageTakenReductionFromEffect => DamageTakenReduction(entry, DamageCause.Effect),
            ContextParameter.DamageTakenReductionFromPassive => DamageTakenReduction(entry, DamageCause.Passive),
            ContextParameter.DotDamageTakenReduction =>
                new DamageBinding(owner => new DotDamageTakenReductionContextModifier(owner, () => entry.Value)),
            ContextParameter.BurningDamageTakenReduction => DotTakenReduction(entry, DamageType.Burning),
            ContextParameter.PoisonDamageTakenReduction => DotTakenReduction(entry, DamageType.Poison),
            ContextParameter.BleedDamageTakenReduction => DotTakenReduction(entry, DamageType.Bleed),
            _ => throw new NotSupportedException($"No binding for context parameter '{entry.Parameter}'")
        };

        private static IContextModifierBinding ElementalConversion(ContextModifierEntry entry, DamageType element) =>
            new DamageBinding(owner => new ElementalConversionContextModifier(owner, element, () => entry.Value));

        private static IContextModifierBinding AddedElemental(ContextModifierEntry entry, DamageType element) =>
            new DamageBinding(owner => new AddedElementalDamageContextModifier(owner, element, () => entry.Value, DamageCause.Attack));

        private static IContextModifierBinding DamageTakenReduction(ContextModifierEntry entry, DamageCause cause) =>
            new DamageBinding(owner => new IncomingDamageReductionContextModifier(owner, entry.Value, cause));

        private static IContextModifierBinding DotTakenReduction(ContextModifierEntry entry, DamageType status) =>
            new DamageBinding(owner => new DotDamageTakenReductionContextModifier(owner, () => entry.Value, status));

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

        private sealed class ActivationBinding(IAbilityActivationModifier modifier) : IContextModifierBinding
        {
            public void Attach(IFightable owner) => owner.ModifierHandler.Add(modifier);

            public void Detach(IFightable owner) => owner.ModifierHandler.Remove(modifier);
        }

        /// <summary>Damage mutators gate on the wearer (the handler sees damage dealt AND taken), so the
        /// modifier can only be built once the owner is known — at attach time, not at binding time.</summary>
        private sealed class DamageBinding(Func<IFightable, IDamageModifier> create) : IContextModifierBinding
        {
            private IDamageModifier? _modifier;

            public void Attach(IFightable owner)
            {
                _modifier = create(owner);
                owner.ModifierHandler.Add(_modifier);
            }

            public void Detach(IFightable owner)
            {
                if (_modifier != null) owner.ModifierHandler.Remove(_modifier);
                _modifier = null;
            }
        }
    }
}
