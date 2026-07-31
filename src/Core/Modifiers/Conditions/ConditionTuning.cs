namespace Core.Modifiers.Conditions
{
    /// <summary>
    /// Tuning shared by the whole threshold family — one decision for every "while below X%" line,
    /// never a per-entry knob. The band is expressed as a fraction of the resource maximum: a line
    /// armed under 30% only disarms above 35%, so alternating damage and healing around the line
    /// resolves to a single flip instead of a blinking parameter and a blinking interface.
    /// </summary>
    public sealed record ConditionTuning
    {
        public const float DefaultResourceThresholdHysteresis = 0.05f;

        /// <summary>The tuning every host uses until one supplies its own.</summary>
        public static ConditionTuning Default { get; } = new();

        public float ResourceThresholdHysteresis { get; init; } = DefaultResourceThresholdHysteresis;
    }
}
