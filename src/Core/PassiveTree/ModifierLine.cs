namespace Core.PassiveTree
{
    using Enums;
    using Modifiers;

    /// <summary>
    /// One player-facing stat line of a node: which parameter, which value bucket, how much.
    /// The value is stored the way the game stores modifier values — a fraction, so 0.08 is +8%
    /// for Increase/Multiplicative and a raw amount for Flat.
    /// </summary>
    public sealed class ModifierLine
    {
        public EntityParameter Parameter { get; set; } = EntityParameter.PhysicalDamage;

        public ModifierValueType ValueType { get; set; } = ModifierValueType.Increase;

        public float Value { get; set; }

        /// <summary>
        /// Id of the predicate that gates the line, from the Conditions catalog; empty means the line
        /// always applies. A reader with a fighter to answer it against resolves the id and counts the
        /// line only while it holds; one without battle state (the editor's summator) counts conditional
        /// lines as if they were always on and flags the total.
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        public bool IsConditional => !string.IsNullOrWhiteSpace(Condition);

        /// <summary>The line as the modifier the game resolves values through, stamped with the tree's
        /// source so a refund can find it again. Written once here rather than at each reader: the
        /// character's live contribution and every summary of an allocation have to mint the same
        /// modifier from the same line.</summary>
        public SimpleModifier ToModifier() => ToModifier(Parameter);

        /// <summary>The same line minted onto another parameter — what an aggregate becomes for a reader
        /// with nothing behind it to fold the family back together.</summary>
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
