namespace Core.Modifiers
{
    using System;
    using Entity;
    using Enums;
    using Interfaces;

    public class SimpleModifier(EntityParameter entityParameter, ModifierValueType valueType, float value, string source, float weight = 1) : IConditionalModifier
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

        /// <summary>Catalog id of the predicate the line was rolled with, kept beside the predicate itself:
        /// it is what the line is saved and compared by (<see cref="ModifierKey"/>), and the predicate
        /// answers for one owner and cannot be asked what it is called.</summary>
        public string? ConditionId { get; set; }

        /// <summary>The predicate, once it has been built for this line — null on a line that always counts.
        /// Whoever carries the line points it at a fighter and lets it go again; while nobody does, it is met
        /// by nothing.</summary>
        public ICondition? Condition { get; set; }

        /// <summary>A line named a predicate and got one: it counts while the predicate holds. A line that
        /// named one and has none counts never — a condition nobody could build must cost the line rather
        /// than hand out the bonus it was written to gate.</summary>
        public bool IsActive => Condition?.IsMet ?? ConditionId == null;

        public IModifierInstance Copy()
        {
            var copy = new SimpleModifier(EntityParameter, ModifierValueType, Value, Source, Weight) { Scope = Scope };
            TransferStamps(this, copy);
            return copy;
        }

        /// <summary>Carries the roll provenance onto a re-minted copy (item copy/expand paths create fresh
        /// instances through ModifiersCreator, which knows nothing about stamps). The predicate travels as a
        /// fresh unattached instance: two copies of one line watch their own owners.</summary>
        public static void TransferStamps(IModifier source, IModifier target)
        {
            if (source is not SimpleModifier from || target is not SimpleModifier to) return;
            to.Affix = from.Affix;
            to.GroupId = from.GroupId;
            to.RolledRange = from.RolledRange;
            to.ConditionId = from.ConditionId;
            to.Condition = from.Condition?.Copy();
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
