namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;

    /// <summary>Impact rider of a data-declared behaviour: builds its effect fresh per touch from the
    /// record's numbers and lays it, filtered by kind of touch. Fresh per touch so the effect reads the
    /// effectiveness of the cast the impact came out of.</summary>
    public class DataEffectImpactRider(
        string augmentId,
        string effectId,
        Func<IAbility, RecordProperties> numbers,
        ImpactKind? kind,
        bool poolFromWholeHit,
        Func<IEffectProvider?> providerAccessor) : IImpactRider
    {
        public string Id => $"Ability_Behaviour_{augmentId}_On_Impact";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(AbilityImpact impact)
        {
            if (!impact.Succeeded) return;
            if (kind != null && impact.Kind != kind) return;

            IEffect? effect = providerAccessor()?.CreateEffect(effectId, numbers(impact.Source));
            if (effect == null) return;

            await effect.Apply(new EffectApplyingContext
            {
                Caster = impact.Caster,
                Target = impact.Target,
                Source = InstanceId,
                Damage = impact.Damage,
                PoolFromWholeHit = poolFromWholeHit,
                IsCritical = impact.IsCritical,
                Effectiveness = impact.Source.Effectiveness,
                Trace = impact.Source.Trace
            });
        }
    }
}
