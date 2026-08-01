namespace Core.Modifiers.Conditions
{
    using Enums;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// "While the one I am hitting sits under X% of a vital" — the target-side counterpart of
    /// <see cref="ResourceThresholdCondition"/>, reading the same share of the same maximum through
    /// <see cref="ResourceCondition.ShareOf"/>.
    /// <para>Unbanded, where the owner-side predicate is banded. That band exists to damp a stream of flips
    /// on a value moving under a live predicate; this one is answered once per attack against whoever is
    /// being hit, so there is no stream to damp and a band would only make the same target read differently
    /// depending on the previous swing. The whole share is usable as a result, up to "anything but full".</para>
    /// <para>A target that has no such vital at all is outside the line either way, exactly as on the owner
    /// side: nobody is low on a barrier he never had, and the inversion flag must not turn that silence into
    /// a verdict.</para>
    /// </summary>
    public sealed class TargetResourceThresholdCondition(Costs resource, float fraction) : TargetCondition
    {
        private float? Share => ResourceCondition.ShareOf(Target, resource);

        protected override bool CanAnswer => base.CanAnswer && Share != null;

        protected override bool Evaluate(bool wasMet) => Share is { } share && share < fraction;
    }

    public class TargetResourceThresholdConditionFactory : ResourceConditionFactory
    {
        /// <summary>A whole resource — the top of the range a share may name. Naming it is "while the target
        /// is not at full", which is a threshold here and not a boundary: the boundary record answers about
        /// the owner and there is nothing on the target side it could be asked of.</summary>
        private const float WholeResource = 1f;

        public override string Type => ConditionTypes.TargetResourceThreshold;

        protected override OwnerCondition? Create(JObject json, Costs resource)
        {
            float fraction = json.Value<float?>(ConditionFields.Value) ?? 0f;
            if (fraction > 0f && fraction <= WholeResource) return new TargetResourceThresholdCondition(resource, fraction);

            Tracker.TrackError($"Skipping condition '{Type}': '{ConditionFields.Value}' is {fraction}, "
                + $"expected a share of the target's maximum in (0..{WholeResource}]");
            return null;
        }
    }
}
