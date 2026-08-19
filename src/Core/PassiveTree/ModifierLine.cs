namespace Core.PassiveTree
{
    using Enums;
    using Modifiers;

    /// <summary>One player-facing stat line of a node: which parameter, which value bucket, how much.
    /// Stored as the game stores modifier values — a fraction (0.08 = +8%) for Increase/Multiplicative, a raw amount for Flat.</summary>
    public sealed class ModifierLine
    {
        public EntityParameter Parameter { get; set; } = EntityParameter.PhysicalDamage;

        public ModifierValueType ValueType { get; set; } = ModifierValueType.Increase;

        public float Value { get; set; }

        /// <summary>Predicate id from the Conditions catalog; empty means always-on. A reader with a
        /// fighter counts it only while it holds; the editor's summator (no battle state) counts it as always-on and flags the total.</summary>
        public string Condition { get; set; } = string.Empty;

        public bool IsConditional => !string.IsNullOrWhiteSpace(Condition);

        /// <summary>The line as the modifier the game resolves values through, stamped with the tree's
        /// source so a refund can find it again — minted once here so every reader mints the same modifier from the same line.</summary>
        public SimpleModifier ToModifier() => ToModifier(Parameter);

        /// <summary>Same line minted onto another parameter — what an aggregate becomes for a reader with
        /// nothing behind it to fold the family back together.</summary>
        public SimpleModifier ToModifier(EntityParameter parameter) =>
            new(parameter, ValueType, Value, PassiveTreeDocument.ModifierSource);

        public ModifierLine Copy() => new()
        {
            Parameter = Parameter,
            ValueType = ValueType,
            Value = Value,
            Condition = Condition
        };
    }
}
