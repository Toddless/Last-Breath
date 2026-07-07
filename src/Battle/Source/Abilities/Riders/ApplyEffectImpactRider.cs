namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Data;

    /// <summary>
    /// Impact rider that puts a fresh copy of the effect on every SUCCESSFUL impact target — a bounce
    /// that lands on three enemies debuffs all three, repeated landings stack. Direct hits always
    /// succeed; evaded/blocked attacks of a series don't apply the effect.
    /// </summary>
    public class ApplyEffectImpactRider(IEffect effect) : IImpactRider
    {
        public string Id => $"Ability_Apply_{effect.Id}_On_Impact";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(AbilityImpact impact)
        {
            if (!impact.Succeeded) return;
            await ApplyEffect(impact);
        }

        private async Task ApplyEffect(AbilityImpact impact) =>
            await effect
                .Copy()
                .Apply(new EffectApplyingContext { Caster = impact.Caster, Source = InstanceId, Target = impact.Target });
    }
}
