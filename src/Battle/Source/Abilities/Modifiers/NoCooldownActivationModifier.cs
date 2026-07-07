namespace Battle.Source.Abilities.Modifiers
{
    using System;
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Cast mutator: the ability activates without a cooldown. Skips the excluded ability (its own source).</summary>
    public class NoCooldownActivationModifier(string excludedAbilityId) : IAbilityActivationModifier
    {
        public string Id => "Context_Modifier_No_Cooldown_Cast";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Priority Priority { get; set; } = Priority.Weak;
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public void Apply(IAbilityActivationContext context)
        {
            if (context.Ability.Id == excludedAbilityId) return;
            context.Cooldown = 0;
        }
    }
}
