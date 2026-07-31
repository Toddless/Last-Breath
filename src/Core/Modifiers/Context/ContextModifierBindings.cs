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
    /// Whole-number knobs (durations, stacks) read the floored value. A knob that carries a number takes it
    /// as a live view from <see cref="ValueViews"/> and never as the number itself: lines are attached on
    /// equip and their value keeps moving afterwards (sharpening, ascension), and nothing re-attaches them.
    /// Taking no view at all is what makes a knob a switch — it has no value to read — and it is the only
    /// mark of one: <see cref="ContextKnobs"/> reads the answer back off this table rather than keeping a
    /// second list of names beside it.</summary>
    internal static class ContextModifierBindings
    {
        public static IContextModifierBinding Create(ContextModifierEntry entry) => Create(entry, new ValueViews());

        public static IContextModifierBinding Create(ContextModifierEntry entry, ValueViews views) => entry.Parameter switch
        {
            ContextParameter.HealingEfficiency => new HealBinding(new HealingBonusContextModifier(views.Of(entry))),
            ContextParameter.BleedDuration => new EffectApplicationBinding(new EffectDurationBonusContextModifier(StatusEffects.Bleed, views.WholeOf(entry))),
            ContextParameter.BleedDamage => new EffectApplicationBinding(new DotDamageBonusContextModifier(StatusEffects.Bleed, views.Of(entry))),
            ContextParameter.BurningStacks => new EffectApplicationBinding(new BonusEffectStacksContextModifier(StatusEffects.Burning, views.WholeOf(entry))),
            ContextParameter.BurningDamage => new EffectApplicationBinding(new DotDamageBonusContextModifier(StatusEffects.Burning, views.Of(entry))),
            ContextParameter.PoisonDamage => new EffectApplicationBinding(new DotDamageBonusContextModifier(StatusEffects.Poison, views.Of(entry))),
            ContextParameter.HealthOnHit => new AttackModifierBinding(new HealthOnHitContextModifier(views.WholeAmountOf(entry))),
            ContextParameter.ManaOnHit => new AttackModifierBinding(new ManaOnHitContextModifier(views.WholeAmountOf(entry))),

            // Mythic marks. Damage knobs gate on the owner, so their modifier is built at attach time.
            ContextParameter.PhysicalToFire => ElementalConversion(entry, views, DamageType.Fire),
            ContextParameter.PhysicalToCold => ElementalConversion(entry, views, DamageType.Cold),
            ContextParameter.PhysicalToLightning => ElementalConversion(entry, views, DamageType.Lightning),

            // The one switch: nothing to read, so no view is taken and the line is on because it exists.
            ContextParameter.AttacksIgnoreResistances =>
                new DamageBinding(owner => new IgnoreResistancesContextModifier(owner, DamageCause.Attack)),
            ContextParameter.AddedFireDamage => AddedElemental(entry, views, DamageType.Fire),
            ContextParameter.AddedColdDamage => AddedElemental(entry, views, DamageType.Cold),
            ContextParameter.AddedLightningDamage => AddedElemental(entry, views, DamageType.Lightning),
            ContextParameter.AttackPureConversion =>
                Damage(entry, views, (owner, value) => new DamageConversionContextModifier(owner, value, DamageCause.Attack)),
            ContextParameter.CooldownResetChance =>
                new ActivationBinding(new ChanceCooldownResetActivationContextModifier(views.Of(entry))),
            ContextParameter.FreeCastChance =>
                new ActivationBinding(new ChanceFreeCastActivationContextModifier(views.Of(entry))),
            ContextParameter.EffectDurationScale =>
                new EffectApplicationBinding(new EffectDurationScaleContextModifier(views.Of(entry))),
            ContextParameter.DamageTakenReductionFromAttack => DamageTakenReduction(entry, views, DamageCause.Attack),
            ContextParameter.DamageTakenReductionFromAbility => DamageTakenReduction(entry, views, DamageCause.Ability),
            ContextParameter.DamageTakenReductionFromEffect => DamageTakenReduction(entry, views, DamageCause.Effect),
            ContextParameter.DamageTakenReductionFromPassive => DamageTakenReduction(entry, views, DamageCause.Passive),
            ContextParameter.DotDamageTakenReduction =>
                Damage(entry, views, (owner, value) => new DotDamageTakenReductionContextModifier(owner, value)),
            ContextParameter.BurningDamageTakenReduction => DotTakenReduction(entry, views, DamageType.Burning),
            ContextParameter.PoisonDamageTakenReduction => DotTakenReduction(entry, views, DamageType.Poison),
            ContextParameter.BleedDamageTakenReduction => DotTakenReduction(entry, views, DamageType.Bleed),
            _ => throw new NotSupportedException($"No binding for context parameter '{entry.Parameter}'")
        };

        /// <summary>An owner-gated damage knob that carries a number. The view is taken here, while the
        /// binding is built, and not inside the attach-time factory: what the table says about a knob has
        /// to be answerable without an entity to attach it to. Taking the view is also what declares the
        /// knob numeric, so an owner-gated switch has no route through here — it needs a
        /// <see cref="DamageBinding"/> built by hand, reading nothing, the way
        /// <see cref="ContextParameter.AttacksIgnoreResistances"/> is.</summary>
        private static IContextModifierBinding Damage(ContextModifierEntry entry, ValueViews views, Func<IFightable, Func<float>, IDamageModifier> create)
        {
            Func<float> value = views.Of(entry);

            return new DamageBinding(owner => create(owner, value));
        }

        private static IContextModifierBinding ElementalConversion(ContextModifierEntry entry, ValueViews views, DamageType element) =>
            Damage(entry, views, (owner, value) => new ElementalConversionContextModifier(owner, element, value));

        private static IContextModifierBinding AddedElemental(ContextModifierEntry entry, ValueViews views, DamageType element) =>
            Damage(entry, views, (owner, value) => new AddedElementalDamageContextModifier(owner, element, value, DamageCause.Attack));

        private static IContextModifierBinding DamageTakenReduction(ContextModifierEntry entry, ValueViews views, DamageCause cause) =>
            Damage(entry, views, (owner, value) => new IncomingDamageReductionContextModifier(owner, value, cause));

        private static IContextModifierBinding DotTakenReduction(ContextModifierEntry entry, ValueViews views, DamageType status) =>
            Damage(entry, views, (owner, value) => new DotDamageTakenReductionContextModifier(owner, value, status));

        /// <summary>Hands out the live views of a line's value and remembers whether any binding asked for
        /// one. A modifier is given the view rather than the number because the line keeps being re-valued
        /// under it; the record of who asked is what lets the table be questioned about a knob's kind
        /// without building a second catalogue that can disagree with it.</summary>
        internal sealed class ValueViews
        {
            /// <summary>False for a switch: its binding had no number to look at.</summary>
            public bool Taken { get; private set; }

            public Func<float> Of(ContextModifierEntry entry)
            {
                Taken = true;

                return () => entry.Value;
            }

            /// <summary>The same view floored, for knobs counted in whole turns or stacks.</summary>
            public Func<int> WholeOf(ContextModifierEntry entry)
            {
                Taken = true;

                return () => entry.WholeValue;
            }

            /// <summary>The floored view again, for a modifier whose contract is a float: the knob is
            /// still counted in whole units (health and mana on hit), the pipeline only adds it into a
            /// total that is not.</summary>
            public Func<float> WholeAmountOf(ContextModifierEntry entry)
            {
                Taken = true;

                return () => entry.WholeValue;
            }
        }

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
