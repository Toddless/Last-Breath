namespace Core.PassiveTree
{
    using Enums;

    /// <summary>
    /// One pipeline knob a node tunes: which <see cref="ContextParameter"/>, which value bucket, how much.
    /// Deliberately data and nothing more — a node never owns a live context modifier. Context pipelines
    /// apply their modifiers one after another instead of summing them, so twenty nodes each wearing
    /// "+10%" separately would compound into a factor nobody authored. What the taken nodes carry is
    /// summed per knob first, and only the total reaches a fighter.
    /// </summary>
    public sealed class ContextModifierLine
    {
        /// <summary>What a flag line is worth. A switch has no number to author: it is on because the
        /// line exists, and no multiplier ever moves it.</summary>
        public const float FlagValue = 1f;

        private float _value;

        public ContextParameter Parameter { get; set; } = ContextParameter.HealingEfficiency;

        public ModifierValueType ValueType { get; set; } = ModifierValueType.Increase;

        /// <summary>A fraction for Increase/Multiplicative, a raw amount for Flat, and always
        /// <see cref="FlagValue"/> on a flag — whatever number was typed before the line became a switch
        /// stays out of everyone's way instead of being read as a quantity.</summary>
        public float Value
        {
            get => IsFlag ? FlagValue : _value;
            set => _value = value;
        }

        /// <summary>
        /// Id of the predicate that gates the line, from the Conditions catalog; empty means the line
        /// always applies. Same shape as the parametric line's condition — both channels resolve it
        /// against the same catalog — but it costs a knob nothing to hold: the knob's total is added up
        /// when a pipeline reads it, so a line that stops holding simply stops being added.
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        public bool IsConditional => !string.IsNullOrWhiteSpace(Condition);

        public bool IsFlag => ValueType == ModifierValueType.Flag;

        public ContextModifierLine Copy() => new()
        {
            Parameter = Parameter,
            ValueType = ValueType,
            Value = Value,
            Condition = Condition
        };
    }
}
