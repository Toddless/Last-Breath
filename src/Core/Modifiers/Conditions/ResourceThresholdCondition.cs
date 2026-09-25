namespace Core.Modifiers.Conditions
{
    using Enums;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// "While the resource sits under X% of its maximum". Comparison is always in shares of the
    /// maximum — a raw amount would mean something different on every fighter.
    /// The switch is banded: it arms strictly below the threshold and disarms only once the resource
    /// climbs a full <see cref="ConditionTuning.ResourceThresholdHysteresis"/> above it, so a fight
    /// that trades damage and healing across the line produces one flip instead of a stream of them.
    /// The band belongs to the raw answer and is applied before the inversion flag, so an inverted
    /// record ("while above X%") arms a full band above the threshold and disarms at the threshold
    /// itself: the gap stays on the same side of the line whichever way the record reads.
    /// A fighter who has no such resource at all is outside the line either way — neither under the
    /// threshold nor above it. "Below X% of a barrier" would otherwise hold on everyone who never had a
    /// barrier, and the inverted "while the barrier is above X%" would hold on exactly the same crowd,
    /// which is nearly everyone; a fighter in the strength stance carries no mana pool the same way.
    /// The vital can appear and vanish mid-fight and the predicate follows its maximum, so the line
    /// arms the moment there is something to be low on and lets go when it is gone. "There is no
    /// barrier" is the boundary record (<see cref="ResourceState.Empty"/>), which answers it directly.
    /// </summary>
    public sealed class ResourceThresholdCondition(Costs resource, float fraction, float hysteresis) : ResourceCondition(resource)
    {
        protected override bool CanAnswer => Fraction != null;

        protected override bool Evaluate(bool wasMet) => Fraction is { } share && share < (wasMet ? fraction + hysteresis : fraction);
    }

    public class ResourceThresholdConditionFactory(ConditionTuning tuning) : ResourceConditionFactory
    {
        public override string Type => ConditionTypes.ResourceThreshold;

        /// <summary>The highest share a banded line can watch: above it the release point would sit past a
        /// full resource, so the line would arm once and never disarm again — including at full health.
        /// A record that wants the very top asks for a boundary, and boundaries are their own predicate.</summary>
        private float Highest => 1f - tuning.ResourceThresholdHysteresis;

        protected override OwnerCondition? Create(JObject json, Costs resource)
        {
            float fraction = json.Value<float?>(ConditionFields.Value) ?? 0f;
            if (fraction > 0f && fraction <= Highest) return new ResourceThresholdCondition(resource, fraction, tuning.ResourceThresholdHysteresis);

            Tracker.TrackError($"Skipping condition '{Type}': '{ConditionFields.Value}' is {fraction}, expected a share of the maximum in (0..{Highest}] — "
                + $"'while not at full' is '{ConditionTypes.ResourceState}' with '{ConditionFields.State}': '{nameof(ResourceState.Full)}' and '{ConditionFields.Negate}': true");
            return null;
        }
    }
}
