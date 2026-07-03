namespace Core.Interfaces.Abilities
{
    using System.Threading.Tasks;

    /// <summary>Reaction fired after the ability finished executing (buffs/debuffs applied on cast).</summary>
    public interface IAbilityPostActivationModifier : IIdentifiable
    {
        Task Apply(IAbilityActivationContext context);
    }
}
