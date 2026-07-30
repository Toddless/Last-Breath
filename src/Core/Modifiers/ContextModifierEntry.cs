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
        public float Value { get; set; } = baseValue;
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
            new(Parameter, ValueType, BaseValue, Weight) { Value = Value, Affix = Affix, GroupId = GroupId, RolledRange = RolledRange };

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
