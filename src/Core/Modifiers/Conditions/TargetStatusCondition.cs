namespace Core.Modifiers.Conditions
{
    using Enums;

    /// <summary>
    /// "While any of these statuses is on the one I am hitting" — the target-side counterpart of
    /// <see cref="StatusCondition"/>, reading the same mask and the same aliases, so a group means one thing
    /// whichever side of the hit it is asked about. The negated form covers "while the target is clean".
    /// </summary>
    public sealed class TargetStatusCondition(StatusEffects mask) : TargetCondition
    {
        protected override bool Evaluate(bool wasMet) => Target is { } target && (target.StatusEffects & mask) != 0;
    }

    public class TargetStatusConditionFactory : StatusMaskConditionFactory
    {
        public override string Type => ConditionTypes.TargetStatus;

        protected override OwnerCondition Create(StatusEffects mask) => new TargetStatusCondition(mask);
    }
}
