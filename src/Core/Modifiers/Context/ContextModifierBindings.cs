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
        /// <summary>Puts the modifier in the slot the line's source asked for, where its class picked none
        /// of its own. Null asks for nothing. Called while the binding is built, so the slot is always
        /// taken before the modifier is registered — a handler sorts on registration.</summary>
        void SlotAt(ContextModifierPriority? priority);

        void Attach(IFightable owner);
        void Detach(IFightable owner);
    }

    /// <summary>Maps a data-level context line to the concrete pipeline modifier it stands for.
    /// Whole-number knobs (durations, stacks) read the floored value. A knob that carries a number takes it
    /// as a live view from <see cref="ValueViews"/> and never as the number itself: lines are attached on
    /// equip and their value keeps moving afterwards (sharpening, ascension), and nothing re-attaches them.
    /// Asking for no amount is what makes a knob a switch — it has no number to read — and it is the only
    /// mark of one: <see cref="ContextKnobs"/> reads the answer back off this table rather than keeping a
    /// second list of names beside it. Every view a binding needs, the amounts and the presence a switch
    /// reads, is taken while the binding is built and never inside an attach-time factory: the table is
    /// read by asking it to build, and a view asked for only once an entity turns up would be asked for by
    /// nothing at all.
    /// The source of a line may also name the slot its modifiers run in, which is taken here — before the
    /// modifier ever reaches a handler, and only where the class named no slot of its own.</summary>
    internal static class ContextModifierBindings
    {
        public static IContextModifierBinding Create(ContextModifierEntry entry) => Create(entry, new ValueViews());

        public static IContextModifierBinding Create(ContextModifierEntry entry, ValueViews views)
        {
            IContextModifierBinding binding = Build(entry, views);
            binding.SlotAt(entry.Priority);

            return binding;
        }

        private static IContextModifierBinding Build(ContextModifierEntry entry, ValueViews views) => entry.Parameter switch
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

            // The one switch: no amount is asked for, so the knob carries no number. What it does read is
            // the presence of its line, which is not an amount and leaves the knob a switch.
            ContextParameter.AttacksIgnoreResistances => Presence(entry, views),
            ContextParameter.AddedFireDamage => AddedElemental(entry, views, DamageType.Fire),
            ContextParameter.AddedColdDamage => AddedElemental(entry, views, DamageType.Cold),
            ContextParameter.AddedLightningDamage => AddedElemental(entry, views, DamageType.Lightning),
            ContextParameter.AttackSacredConversion =>
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
        /// knob numeric, so an owner-gated switch has no route through here — it reads a presence instead,
        /// through <see cref="Presence"/>.</summary>
        private static IContextModifierBinding Damage(ContextModifierEntry entry, ValueViews views, Func<IFightable, Func<float>, IDamageModifier> create)
        {
            Func<float> value = views.Of(entry);

            return new DamageBinding(owner => create(owner, value));
        }

        /// <summary>An owner-gated switch: <see cref="Damage"/> with a presence where the amount would be.
        /// The view is taken here for the same reason the amount is — the table is questioned by asking it
        /// to build a binding, and a view the attach-time factory would ask for is one the question never
        /// reaches. Presence is not an amount, so taking it leaves the knob a switch.</summary>
        private static IContextModifierBinding Presence(ContextModifierEntry entry, ValueViews views)
        {
            Func<bool> isOn = views.PresenceOf(entry);

            return new DamageBinding(owner => new IgnoreResistancesContextModifier(owner, DamageCause.Attack, isOn));
        }

        private static IContextModifierBinding ElementalConversion(ContextModifierEntry entry, ValueViews views, DamageType element) =>
            Damage(entry, views, (owner, value) => new ElementalConversionContextModifier(owner, element, value));

        private static IContextModifierBinding AddedElemental(ContextModifierEntry entry, ValueViews views, DamageType element) =>
            Damage(entry, views, (owner, value) => new AddedElementalDamageContextModifier(owner, element, value, DamageCause.Attack));

        private static IContextModifierBinding DamageTakenReduction(ContextModifierEntry entry, ValueViews views, DamageCause cause) =>
            Damage(entry, views, (owner, value) => new IncomingDamageReductionContextModifier(owner, value, cause));

        private static IContextModifierBinding DotTakenReduction(ContextModifierEntry entry, ValueViews views, DamageType status) =>
            Damage(entry, views, (owner, value) => new DotDamageTakenReductionContextModifier(owner, value, status));

        /// <summary>Moves a freshly built modifier into the slot the line's source named — and only where
        /// the class left itself in the default one. A class that picked a slot picked it for a rule that
        /// stands on it (a conversion has to see the final number, so nothing may run after it), while a
        /// source names a slot to order the modifiers it hands out as a group; the group order is the
        /// weaker claim of the two and yields wherever they meet. That the class expressed no preference is
        /// read off the instance the table has just built, so no list of which knobs mind is kept anywhere.
        /// Everything the table builds is a <see cref="ContextModifier"/>; anything else keeps whatever
        /// order it declares for itself rather than being forced into one it cannot carry.</summary>
        private static void Slot(object modifier, ContextModifierPriority? priority)
        {
            if (priority is not { } slot || modifier is not ContextModifier context) return;
            if (context.Priority != ContextModifierPriority.Normal) return;

            context.Priority = slot;
        }

        /// <summary>Hands out the live views of a line's value and remembers whether any binding asked for
        /// an amount. A modifier is given the view rather than the number because the line keeps being
        /// re-valued under it; the record of who asked for what is what lets the table be questioned about
        /// a knob's kind without building a second catalogue that can disagree with it.</summary>
        internal sealed class ValueViews
        {
            /// <summary>False for a switch: its binding had no number to look at.</summary>
            public bool Taken { get; private set; }

            /// <summary>True when the view handed out was a floored one: the knob is counted in whole
            /// units and the fraction of its value never reaches a pipeline. Recorded for the same reason
            /// as <see cref="Taken"/> — the table is the only place that decides it.</summary>
            public bool Whole { get; private set; }

            public Func<float> Of(ContextModifierEntry entry)
            {
                Taken = true;

                return () => entry.Value;
            }

            /// <summary>The same view floored, for knobs counted in whole turns or stacks.</summary>
            public Func<int> WholeOf(ContextModifierEntry entry)
            {
                Taken = true;
                Whole = true;

                return () => entry.WholeValue;
            }

            /// <summary>The floored view again, for a modifier whose contract is a float: the knob is
            /// still counted in whole units (health and mana on hit), the pipeline only adds it into a
            /// total that is not.</summary>
            public Func<float> WholeAmountOf(ContextModifierEntry entry)
            {
                Taken = true;
                Whole = true;

                return () => entry.WholeValue;
            }

            /// <summary>Whether the switch is on, for a binding that asks for no amount. Deliberately
            /// leaves <see cref="Taken"/> alone: this is not a number the pipeline scales anything by, and
            /// a knob that answered it would stop being a switch — the one it is on today would move out
            /// of <see cref="ContextKnobs.Flags"/> and both readers would start refusing the flag lines
            /// that are already written for it. A line pinned to one is on, which is what a switch that
            /// simply exists comes to; a source whose switch can go quiet (a line held up by a condition)
            /// answers zero and the modifier stands down without being detached.</summary>
            public Func<bool> PresenceOf(ContextModifierEntry entry) => () => entry.Value != 0f;
        }

        private sealed class HealBinding(IHealModifier modifier) : IContextModifierBinding
        {
            public void SlotAt(ContextModifierPriority? priority) => Slot(modifier, priority);
            public void Attach(IFightable owner) => owner.ModifierHandler.Add(modifier);
            public void Detach(IFightable owner) => owner.ModifierHandler.Remove(modifier);
        }

        private sealed class EffectApplicationBinding(IEffectApplicationModifier modifier) : IContextModifierBinding
        {
            public void SlotAt(ContextModifierPriority? priority) => Slot(modifier, priority);
            public void Attach(IFightable owner) => owner.ModifierHandler.Add(modifier);
            public void Detach(IFightable owner) => owner.ModifierHandler.Remove(modifier);
        }

        private sealed class AttackModifierBinding(IAttackModifier modifier) : IContextModifierBinding
        {
            public void SlotAt(ContextModifierPriority? priority) => Slot(modifier, priority);

            public void Attach(IFightable owner) => owner.ModifierHandler.Add(modifier);

            public void Detach(IFightable owner) => owner.ModifierHandler.Remove(modifier);
        }

        private sealed class ActivationBinding(IAbilityActivationModifier modifier) : IContextModifierBinding
        {
            public void SlotAt(ContextModifierPriority? priority) => Slot(modifier, priority);

            public void Attach(IFightable owner) => owner.ModifierHandler.Add(modifier);

            public void Detach(IFightable owner) => owner.ModifierHandler.Remove(modifier);
        }

        /// <summary>Damage mutators gate on the wearer (the handler sees damage dealt AND taken), so the
        /// modifier can only be built once the owner is known — at attach time, not at binding time.</summary>
        private sealed class DamageBinding(Func<IFightable, IDamageModifier> create) : IContextModifierBinding
        {
            private IDamageModifier? _modifier;
            private ContextModifierPriority? _priority;

            /// <summary>Remembered rather than applied: there is no modifier to place until the owner is
            /// known, and the slot is taken on the instance the attach builds.</summary>
            public void SlotAt(ContextModifierPriority? priority) => _priority = priority;

            public void Attach(IFightable owner)
            {
                _modifier = create(owner);
                Slot(_modifier, _priority);
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
