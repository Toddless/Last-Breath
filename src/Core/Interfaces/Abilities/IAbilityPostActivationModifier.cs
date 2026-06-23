namespace Core.Interfaces.Abilities
{
    using System.Threading.Tasks;

    public interface IAbilityPostActivationModifier : IIdentifiable
    {
        Task Apply(AbilityActivationContext context);
    }
}
