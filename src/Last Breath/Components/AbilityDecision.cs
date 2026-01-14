namespace LastBreath.Components
{
    using LastBreath.Components.Interfaces;
    using LastBreath.Script.Abilities.Interfaces;

    public class AbilityDecision(IAbility ability, float priority) : IAbilityDecision
    {
        public IAbility Ability { get; set; } = ability;

        public float Priority { get; set; } = priority;
    }
}
