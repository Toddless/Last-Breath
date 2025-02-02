namespace Playground.Components
{
    using Playground.Script.Effects.Interfaces;

    public class AbilityDecision(IAbility ability, float priority)
    {
        public IAbility Ability { get; set; } = ability;

        public float Priority { get; set; } = priority;
    }
}
