namespace Core.Interfaces.Abilities
{
    /// <summary>Mutates the cast context (cost, cooldown, targets) before resources are consumed.</summary>
    public interface IAbilityActivationModifier : IContextModifier<IAbilityActivationContext>;
}
