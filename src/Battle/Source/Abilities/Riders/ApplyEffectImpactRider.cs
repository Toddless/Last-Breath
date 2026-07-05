namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// Impact rider that puts a fresh copy of the effect on every impact target — a bounce that
    /// lands on three enemies debuffs all three, repeated landings stack.
    /// </summary>
    public class ApplyEffectImpactRider(IEffect effect) : IImpactRider
    {
        public string Id => $"Ability_Apply_{effect.Id}_On_Impact";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(AbilityImpact impact) =>
            await effect
                .Copy()
                .Apply(new EffectApplyingContext { Caster = impact.Caster, Source = InstanceId, Target = impact.Target });
    }
}
