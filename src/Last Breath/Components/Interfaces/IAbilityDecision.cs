namespace Playground.Components.Interfaces
{
    using Playground.Script.Effects.Interfaces;

    public interface IAbilityDecision
    {
        IAbility Ability { get; set; }
        float Priority { get; set; }
    }
}