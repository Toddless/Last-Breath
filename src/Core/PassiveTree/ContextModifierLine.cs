namespace Core.PassiveTree
{
    using Enums;

    /// <summary>One pipeline knob a node tunes: which <see cref="ContextParameter"/>, which value bucket,
    /// how much. Plain data — a node never owns a live context modifier. Context pipelines apply
    /// modifiers sequentially rather than summing them, so twenty "+10%" nodes applied separately would
    /// compound; what taken nodes carry is summed per knob first, and only the total reaches a fighter.</summary>
    public sealed class ContextModifierLine
    {
        /// <summary>What a flag line is worth. A switch has no number to author: it is on because the
        /// line exists, and no multiplier ever moves it.</summary>
        public const float FlagValue = 1f;

        private float _value;

        public ContextParameter Parameter { get; set; } = ContextParameter.HealingEfficiency;

        public ModifierValueType ValueType { get; set; } = ModifierValueType.Increase;

        /// <summary>A fraction for Increase/Multiplicative, a raw amount for Flat, always
        /// <see cref="FlagValue"/> on a flag — a pre-switch typed number is never read as a quantity.</summary>
        public float Value
        {
            get => IsFlag ? FlagValue : _value;
            set => _value = value;
        }

        /// <summary>Predicate id from the Conditions catalog; empty means always-on — same shape as the
        /// parametric line's condition, but costs a knob nothing: a line that stops holding just stops being added.</summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>Composite stamp shared with the parametric channel: records carrying it are ONE
        /// player-facing line and cost the node one slot together. Null on a line that stands alone.</summary>
        public string? GroupId
        {
            get => field;
            set => field = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public bool IsConditional => !string.IsNullOrWhiteSpace(Condition);

        public bool IsFlag => ValueType == ModifierValueType.Flag;

        public ContextModifierLine Copy() => new()
        {
            Parameter = Parameter,
            ValueType = ValueType,
            Value = Value,
            Condition = Condition,
            GroupId = GroupId
        };
    }
}
