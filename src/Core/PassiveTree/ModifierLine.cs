namespace Core.PassiveTree
{
    using Enums;

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

        public ModifierLine Copy() => new()
        {
            Parameter = Parameter,
            ValueType = ValueType,
            Value = Value,
            Condition = Condition
        };
    }
}
