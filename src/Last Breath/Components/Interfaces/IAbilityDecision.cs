using Playground.Script.Effects.Interfaces;

namespace Playground.Components.Interfaces
{
    public interface IAbilityDecision
    {
        IAbility Ability { get; set; }
        float Priority { get; set; }
    }
}