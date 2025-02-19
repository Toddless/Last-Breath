namespace Playground.Components
{
    using Playground.Components.Interfaces;
    using Playground.Script.Effects.Interfaces;

    public class AbilityDecision(IAbility ability, float priority) : IAbilityDecision
    {
        public IAbility Ability { get; set; } = ability;

        public float Priority { get; set; } = priority;
    }
}
