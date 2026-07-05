namespace Core.Interfaces.Abilities
{
    using System.Threading.Tasks;

    /// <summary>
    /// Rider fired once per cast, after the ability finished executing (buffs/debuffs applied on cast).
    /// Counterpart of <see cref="IImpactRider"/>, which fires per impact instead.
    /// </summary>
    public interface IActivationRider : IIdentifiable
    {
        Task Apply(IAbilityActivationContext context);
    }
}
