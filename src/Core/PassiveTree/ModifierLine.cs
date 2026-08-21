namespace Core.PassiveTree
{
    using System;
    using Enums;
    using Modifiers;

    /// <summary>One player-facing stat line of a node: which parameter, which value bucket, how much.
    /// Stored as the game stores modifier values — a fraction (0.08 = +8%) for Increase/Multiplicative, a raw amount for Flat.</summary>
    public sealed class ModifierLine
    {
        public EntityParameter Parameter { get; set; } = EntityParameter.PhysicalDamage;

        public ModifierValueType ValueType { get; set; } = ModifierValueType.Increase;

        public float Value { get; set; }

        /// <summary>Carrier parameter the value is counted PER UNIT of: 0.01 per Strength is worth +1% for
        /// every point of Strength he holds. Null on an ordinary line, which is worth its value outright.</summary>
        public EntityParameter? PerParameter { get; set; }

        /// <summary>Predicate id from the Conditions catalog; empty means always-on. A reader with a
        /// fighter counts it only while it holds; the editor's summator (no battle state) counts it as always-on and flags the total.</summary>
        public string Condition { get; set; } = string.Empty;

        public bool IsConditional => !string.IsNullOrWhiteSpace(Condition);

        /// <summary>The value is measured off a carrier, so the line is worth nothing until one is known.</summary>
        public bool IsScaled => PerParameter is not null;

        /// <summary>The line as the modifier the game resolves values through, stamped with the tree's
        /// source so a refund can find it again — minted once here so every reader mints the same modifier
        /// from the same line. A scaled line is minted by the contribution, which has a carrier to measure.</summary>
        public SimpleModifier ToModifier() => ToModifier(Parameter);

        /// <summary>Same line minted onto another parameter — what an aggregate becomes for a reader with
        /// nothing behind it to fold the family back together. A scaled line is refused rather than minted
        /// flat: its authored number is a rate, and a modifier carrying it would hand out the whole rate as
        /// an outright bonus.</summary>
        public SimpleModifier ToModifier(EntityParameter parameter) => IsScaled
            ? throw new InvalidOperationException($"'{Parameter}' is measured per unit of {PerParameter} — only a reader with a carrier can mint it")
            : new SimpleModifier(parameter, ValueType, Value, PassiveTreeDocument.ModifierSource);

        public ModifierLine Copy() => new()
        {
            Parameter = Parameter,
            ValueType = ValueType,
            Value = Value,
            PerParameter = PerParameter,
            Condition = Condition
        };
    }
}
