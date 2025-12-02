namespace Battle.TestData.Abilities.Conditions
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class TargetHasStatusCondition(string id, StatusEffects requiredStatus) : ICondition
    {
        private StatusEffects RequiredStatus => requiredStatus;
        public string Id { get; } = id;

        public bool IsMet(EffectApplyingContext context) => context.Target.StatusEffects.HasFlag(RequiredStatus);
    }
}
