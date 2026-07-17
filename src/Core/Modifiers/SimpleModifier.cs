namespace Core.Modifiers
{
    using System;
    using Entity;
    using Enums;

    public class SimpleModifier(EntityParameter entityParameter, ModifierValueType valueType, float value, string source, float weight = 1) : IModifierInstance
    {
        public EntityParameter EntityParameter { get; } = entityParameter;
        public ModifierValueType ModifierValueType { get; } = valueType;
        public ModifierScope Scope { get; set; } = ModifierScope.Global;
        public float Value { get; set; } = value;
        public float BaseValue { get; } = value;
        public string Source { get; } = source;
        public float Weight { get; set; } = weight;
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        // Roll provenance, stamped by the materializer. GroupId ties the parts of one composite roll
        // (they render as one line); RolledRange is the source spread — null when the value was fixed.
        public AffixKind Affix { get; set; }
        public string? GroupId { get; set; }
        public ValueRange? RolledRange { get; set; }

        public IModifierInstance Copy() =>
            new SimpleModifier(EntityParameter, ModifierValueType, Value, Source, Weight) { Scope = Scope, Affix = Affix, GroupId = GroupId, RolledRange = RolledRange };

        /// <summary>Carries the roll provenance onto a re-minted copy (item copy/expand paths create fresh
        /// instances through ModifiersCreator, which knows nothing about stamps).</summary>
        public static void TransferStamps(IModifier source, IModifier target)
        {
            if (source is not SimpleModifier from || target is not SimpleModifier to) return;
            to.Affix = from.Affix;
            to.GroupId = from.GroupId;
            to.RolledRange = from.RolledRange;
        }

        public void ApplyTo(IFightable target) => target.ParameterModifiers.AddModifier(this);

        public void RemoveFrom(IFightable target) => target.ParameterModifiers.RemoveModifier(this);

        public override bool Equals(object? obj)
        {
            if (obj is not IModifier other) return false;
            return EntityParameter == other.EntityParameter && ModifierValueType == other.ModifierValueType;
        }

        public override int GetHashCode() => HashCode.Combine(EntityParameter, ModifierValueType);
    }
}
