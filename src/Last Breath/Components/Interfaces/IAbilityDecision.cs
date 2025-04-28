namespace Playground.Components.Interfaces
{
    using Playground.Script.Abilities.Interfaces;

    public interface IAbilityDecision
    {
        IAbility Ability { get; set; }
        float Priority { get; set; }
    }
}