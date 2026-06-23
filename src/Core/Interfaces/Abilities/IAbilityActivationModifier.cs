namespace Core.Interfaces.Abilities
{
    public interface IAbilityActivationModifier : IIdentifiable
    {
        void Apply(AbilityActivationContext context);
    }
}
