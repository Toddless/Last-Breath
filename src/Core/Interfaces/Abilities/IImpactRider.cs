namespace Core.Interfaces.Abilities
{
    using System.Threading.Tasks;
    using Data;

    /// <summary>
    /// Rider fired on EVERY delivery impact of an ability (each hit, bounce landing or attack of a
    /// series) — unlike <see cref="IActivationRider"/>, which fires once per cast. Applies its payload
    /// to the impact's actual target, so multi-target deliveries affect every target they touch.
    /// </summary>
    public interface IImpactRider : IIdentifiable
    {
        Task Apply(AbilityImpact impact);
    }
}
