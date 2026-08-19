namespace Core.Battle.Abilities
{
    using System.Threading.Tasks;
    using Interfaces;

    /// <summary>
    /// Rider fired once per cast, after the ability finished executing (buffs/debuffs applied on cast).
    /// Counterpart of <see cref="IImpactRider"/>, which fires per impact instead.
    /// </summary>
    public interface IActivationRider : IIdentifiable
    {
        Task Apply(IAbilityActivationContext context);

        /// <summary>Lets go of everything the rider hooked while it worked — see
        /// <see cref="IImpactRider.Detach"/>, same contract on the once-per-cast side.</summary>
        void Detach()
        {
        }
    }
}
