namespace Core.Modifiers
{
    using System;
    using Context;
    using Entity;
    using Enums;
    using Interfaces;

    /// <summary>An item line tuning a context pipeline ("healing efficiency +15%", "+1 bleed duration").
    /// Deliberately not an <see cref="IModifier"/>: context knobs never enter parameter resolution —
    /// the item attaches them to the owner's ModifierHandler on equip. Value scales with the item's
    /// update multiplier like any other line; whole-number knobs floor the scaled value.</summary>
    public class ContextModifierEntry(ContextParameter parameter, ModifierValueType valueType, float baseValue, float weight = 0f) : IWeightable
    {
        private IContextModifierBinding? _binding;
        private IFightable? _boundOwner;

        // Session-local identity for reroll targeting (regenerated on Copy, like entity modifier instances).
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public ContextParameter Parameter { get; } = parameter;
        public ModifierValueType ValueType { get; } = valueType;
        public float BaseValue { get; } = baseValue;

        /// <summary>What the line is worth right now. Normally its own number, moved by whatever re-values
        /// it; when the amount is computed elsewhere (<see cref="Live"/>) the stored number steps aside, so
        /// every reader keeps asking one property whichever kind of line it holds.</summary>
        public float Value
        {
            get => Live is null ? field : Live();
            set => field = value;
        } = baseValue;

        /// <summary>Set when the amount belongs to someone else and has to be answered for at the moment it
        /// is read — a total that several contributors add up to, and that must not be a snapshot taken when
        /// the line was bound.</summary>
        public Func<float>? Live { get; init; }

        /// <summary>The slot the line's source asks the pipeline modifier to run in, so everything one
        /// source hands out can be ordered as a group. Null — the ordinary case — asks for nothing. The ask
        /// is a default and not an override: a modifier class that picked a slot for its own rule keeps it,
        /// which leaves the knobs that must run late or last where they put themselves however they are
        /// handed over.</summary>
        public ContextModifierPriority? Priority { get; init; }

        public float Weight { get; set; } = weight;

        // Roll provenance, stamped by the materializer (see SimpleModifier for the field contract).
        public AffixKind Affix { get; set; }
        public string? GroupId { get; set; }
        public ValueRange? RolledRange { get; set; }

        /// <summary>Floored view for whole-number knobs (durations, stacks).</summary>
        public int WholeValue => (int)Value;

        /// <summary>Binds the line to an owner's context pipelines. An entry lives on ONE owner at a time:
        /// a repeated attach (a passive node taken again, an item re-equipped without an unequip) releases the
        /// previous binding first, and attaching to another owner MOVES the line instead of duplicating it —
        /// the binding is the only handle on the modifier that was added, so dropping it without a detach would
        /// leave the modifier on the entity for good.</summary>
        public void Attach(IFightable owner)
        {
            Release();
            _binding = ContextModifierBindings.Create(this);
            _binding.Attach(owner);
            _boundOwner = owner;
        }

        /// <summary>Releases the line from <paramref name="owner"/>. Does nothing when the line is unbound or
        /// bound elsewhere: a previous owner was already released by the re-attach, so its late detach must not
        /// strip the current one.</summary>
        public void Detach(IFightable owner)
        {
            if (ReferenceEquals(_boundOwner, owner)) Release();
        }

        public ContextModifierEntry Copy() =>
            new(Parameter, ValueType, BaseValue, Weight)
            {
                Value = Value, Affix = Affix, GroupId = GroupId, RolledRange = RolledRange, Live = Live, Priority = Priority
            };

        /// <summary>Drops the current binding off the owner it was added to. The recorded owner is the only
        /// correct target: the modifier instance sits in that entity's handler, whoever asked for the release.</summary>
        private void Release()
        {
            if (_boundOwner != null) _binding?.Detach(_boundOwner);
            _binding = null;
            _boundOwner = null;
        }
    }
}
