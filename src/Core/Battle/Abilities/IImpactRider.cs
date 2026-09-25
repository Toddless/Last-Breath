namespace Core.Battle.Abilities
{
    using System.Threading.Tasks;
    using Data;
    using Interfaces;

    /// <summary>
    /// Rider fired on EVERY delivery impact of an ability (each hit, bounce landing or attack of a
    /// series) — unlike <see cref="IActivationRider"/>, which fires once per cast. Applies its payload
    /// to the impact's actual target, so multi-target deliveries affect every target they touch.
    /// </summary>
    public interface IImpactRider : IIdentifiable
    {
        Task Apply(AbilityImpact impact);

        /// <summary>
        /// Lets go of everything the rider hooked while it worked. Called by the ability the moment the
        /// rider leaves it — an unseated augment, and every rebuild of the binder, which tears the whole
        /// arrangement down before putting it back up.
        ///
        /// <para>Nothing to let go of is the ordinary case, which is why this does nothing by default. A
        /// rider that subscribed to a bus and does not override it leaves a handler on a fighter who will
        /// call it for the rest of the battle, and one more of them with every rebuild.</para>
        /// </summary>
        void Detach()
        {
        }
    }
}
