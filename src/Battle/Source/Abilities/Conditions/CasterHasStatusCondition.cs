namespace Battle.Source.Abilities.Conditions
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class CasterHasStatusCondition(string id, StatusEffects requiredStatus) : ICondition
    {
        private StatusEffects RequiredStatus => requiredStatus;
        public string Id { get; } = id;
        public bool IsMet(EffectApplyingContext context) => context.Caster.StatusEffects.HasFlag(requiredStatus);
    }
}
