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

        public void Attach(IFightable owner)
        {
            _binding = ContextModifierBindings.Create(this);
            _binding.Attach(owner);
        }

        public void Detach(IFightable owner)
        {
            _binding?.Detach(owner);
            _binding = null;
        }

        public ContextModifierEntry Copy() =>
            new(Parameter, ValueType, BaseValue, Weight) { Value = Value, Affix = Affix, GroupId = GroupId, RolledRange = RolledRange };
    }
}
